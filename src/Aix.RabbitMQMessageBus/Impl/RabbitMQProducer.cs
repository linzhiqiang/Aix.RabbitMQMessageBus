using Aix.RabbitMQMessageBus.Foundation;
using Aix.RabbitMQMessageBus.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Aix.RabbitMQMessageBus.Impl
{
    internal class RabbitMQProducer : IRabbitMQProducer
    {
        private IServiceProvider _serviceProvider;
        private ILogger<RabbitMQProducer> _logger;
        private RabbitMQMessageBusOptions _options;
        ObjectPool<ChannelScope> _channelPool = new ObjectPool<ChannelScope>();
        IConnection _connection;
        IModel _channel;
        public RabbitMQProducer(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            _logger = _serviceProvider.GetService<ILogger<RabbitMQProducer>>();
            _options = _serviceProvider.GetService<RabbitMQMessageBusOptions>();

            _connection = _serviceProvider.GetService<IConnection>();
            _channel = _connection.CreateModel();

            if (_options.ConfirmSelect) _channel.ConfirmSelect();

            for (int i = 0; i < _options.ChannelPoolSize; i++)
            {
                var channel = _connection.CreateModel();
                if (_options.ConfirmSelect) channel.ConfirmSelect();
                _channelPool.AddObject(new ChannelScope(_channelPool, channel));
            }

        }

        private IBasicProperties CreateBasicProperties()
        {
            var basicProperties = _channel.CreateBasicProperties();
            basicProperties = _channel.CreateBasicProperties();
            basicProperties.ContentType = "text/plain";
            basicProperties.DeliveryMode = 2;
            basicProperties.Headers = new Dictionary<string, object>();
            return basicProperties;
        }

        public bool ProduceAsync(string topic, byte[] data)
        {
            var exchange = Helper.GeteExchangeName(topic);
            var routingKey = Helper.GeteRoutingKey(topic, "");
            var basicProperties = CreateBasicProperties();

            using (var scope = _channelPool.BorrowObject())
            {
                scope.Channel.BasicPublish(exchange: exchange,
                                              routingKey: routingKey,
                                              basicProperties: basicProperties,
                                              body: data);

                var isOk = true;
                if (_options.ConfirmSelect)
                {
                    isOk = scope.Channel.WaitForConfirms();
                }
                return isOk;
            }
        }

        public bool ProduceDelayAsync(string topic, byte[] data, TimeSpan delay)
        {
            if (delay <= TimeSpan.Zero)
            {
                return ProduceAsync(topic, data);
            }
            var exchange = Helper.GeteDelayExchangeName(_options);
            var delayTopic = Helper.GetDelayTopic(_options);
            var routingKey = Helper.GeteRoutingKey(delayTopic, "");

            var basicProperties = CreateBasicProperties();
            basicProperties.Headers.Add("x-delay", (int)delay.TotalMilliseconds);  //对于生产者 只需这里设置延迟时间即可 单位是毫秒
            using (var scope = _channelPool.BorrowObject())
            {
                scope.Channel.BasicPublish(exchange: exchange,
                                              routingKey: routingKey,
                                              basicProperties: basicProperties,
                                              body: data);

                var isOk = true;
                if (_options.ConfirmSelect)
                {
                    isOk = scope.Channel.WaitForConfirms();
                }
                return isOk;
            }
        }


        public bool ErrorReProduceAsync(string topic, string groupId, byte[] data)
        {
            var exchange = Helper.GetErrorReEnqueneExchangeName(topic); //错误重试的任务 进入的交换器(根据groupid进入对应的队列)
            var routingKey = Helper.GeteRoutingKey(topic, groupId);
            var basicProperties = CreateBasicProperties();
            using (var scope = _channelPool.BorrowObject())
            {
                scope.Channel.BasicPublish(exchange: exchange,
                                                  routingKey: routingKey,
                                                  basicProperties: basicProperties,
                                                  body: data);

                var isOk = true;
                if (_options.ConfirmSelect)
                {
                    isOk = scope.Channel.WaitForConfirms();
                }
                return isOk;
            }
        }
        public void Dispose()
        {
            _logger.LogInformation("RabbitMQ关闭生产者");
            if (this._channel != null)
            {
                With.NoException(_logger, () => { this._channel.Dispose(); }, "RabbitMQ关闭生产者");
            }
        }
      
    }


}
