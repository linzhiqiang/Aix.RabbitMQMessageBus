using Aix.RabbitMQMessageBus.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;

namespace Aix.RabbitMQMessageBus.Impl
{
    internal class RabbitMQProducer : IRabbitMQProducer
    {
        private IServiceProvider _serviceProvider;
        private ILogger<RabbitMQProducer> _logger;
        private RabbitMQMessageBusOptions _options;

        IConnection _connection;
        IModel _channel;
        IBasicProperties _basicProperties;
        public RabbitMQProducer(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            _logger = _serviceProvider.GetService<ILogger<RabbitMQProducer>>();
            _options = _serviceProvider.GetService<RabbitMQMessageBusOptions>();

            _connection = _serviceProvider.GetService<IConnection>();
            _channel = _connection.CreateModel();

            if (_options.ConfirmSelect) _channel.ConfirmSelect();

            _basicProperties = _channel.CreateBasicProperties();
            _basicProperties.ContentType = "text/plain";
            _basicProperties.DeliveryMode = 2;
        }
        public bool ProduceAsync(string topic, byte[] data)
        {
            var exchange = Helper.GeteExchangeName(topic);
            var routingKey = Helper.GeteRoutingKey(topic,"");

            _channel.BasicPublish(exchange: exchange,
                                              routingKey: routingKey,
                                              basicProperties: _basicProperties,
                                              body: data);

            var isOk = true;
            if (_options.ConfirmSelect)
            {
                isOk = _channel.WaitForConfirms();
            }
            return isOk;
        }

        public bool ProduceDelayAsync(string topic,byte[] data, TimeSpan delay)
        {
            if (delay <= TimeSpan.Zero)
            {
                return ProduceAsync(topic, data);
            }
            var exchange = Helper.GeteDelayExchangeName(_options);
            var delayTopic = Helper.GetDelayTopic(_options,delay);
            var routingKey = Helper.GeteRoutingKey(delayTopic,"");
        
            _channel.BasicPublish(exchange: exchange,
                                              routingKey: routingKey,
                                              basicProperties: _basicProperties,
                                              body: data);

            var isOk = true;
            if (_options.ConfirmSelect)
            {
                isOk = _channel.WaitForConfirms();
            }
            return isOk;
        }


        public bool ErrorReProduceAsync(string topic, string groupId, byte[] data)
        {
            var exchange = Helper.GetErrorReEnqueneExchangeName(topic);
            var routingKey = Helper.GeteRoutingKey(topic, groupId);

            _channel.BasicPublish(exchange: exchange,
                                              routingKey: routingKey,
                                              basicProperties: _basicProperties,
                                              body: data);

            var isOk = true;
            if (_options.ConfirmSelect)
            {
                isOk = _channel.WaitForConfirms();
            }
            return isOk;
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
