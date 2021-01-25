using Aix.RabbitMQMessageBus;
using Aix.RabbitMQMessageBus.Impl;
using Aix.RabbitMQMessageBus.Model;
using Aix.RabbitMQMessageBus.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aix.RabbitMQMessageBus
{
    /// <summary>
    /// rabbitMQ实现发布订阅模式
    /// </summary>
    public class RabbitMQMessageBus : IRabbitMQMessageBus
    {
        private IServiceProvider _serviceProvider;
        private ILogger<RabbitMQMessageBus> _logger;
        private RabbitMQMessageBusOptions _options;

        IConnection _connection;
        IRabbitMQProducer _producer;
        List<IDisposable> _consumers = new List<IDisposable>();
        IRabbitMQDelayConsumer _delayQueueConsumer;
        private HashSet<string> Subscribers = new HashSet<string>();
        private volatile bool _isInitDelayQueue = false;

        public RabbitMQMessageBus(IServiceProvider serviceProvider, ILogger<RabbitMQMessageBus> logger, RabbitMQMessageBusOptions options)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _options = options;

            _connection = _serviceProvider.GetService<IConnection>();
            this._producer = new RabbitMQProducer(this._serviceProvider);
        }

        public Task PublishAsync(Type messageType, object message)
        {
            AssertUtils.IsNotNull(message, "消息不能null");
            var topic = GetTopic(messageType);
            var wrapMessage = new RabbitMessageBusData { Type = topic, Data = _options.Serializer.Serialize(message), ExecuteTimeStamp = DateUtils.GetTimeStamp(DateTime.Now) };
            var data = _options.Serializer.Serialize(wrapMessage);
            this._producer.ProduceAsync(topic, data);
            return Task.CompletedTask;
        }

        public async Task PublishDelayAsync(Type messageType, object message, TimeSpan delay)
        {
            AssertUtils.IsNotNull(message, "消息不能null");
            if (delay > TimeSpan.Zero)
            { //加入延迟队列
                var topic = GetTopic(messageType);
                var wrapMessage = new RabbitMessageBusData { Type = topic, Data = _options.Serializer.Serialize(message), ExecuteTimeStamp = DateUtils.GetTimeStamp(DateTime.Now.Add(delay)) };
                var data = _options.Serializer.Serialize(wrapMessage);
                this._producer.ProduceDelayAsync(topic, data, delay);
            }
            else
            {
                await this.PublishAsync(messageType, message);
            }
        }

        public async Task SubscribeAsync<T>(Func<T, Task<bool>> handler, SubscribeOptions subscribeOptions = null, CancellationToken cancellationToken = default(CancellationToken)) where T : class
        {
            InitDelayQueue();
            var topic = GetTopic(typeof(T));

            var groupId = subscribeOptions?.GroupId;

            var threadCount = subscribeOptions?.ConsumerThreadCount ?? 0;
            threadCount = threadCount > 0 ? threadCount : _options.DefaultConsumerThreadCount;
            AssertUtils.IsTrue(threadCount > 0, "消费者线程数必须大于0");

            var key = !string.IsNullOrEmpty(groupId) ? $"{topic}_{groupId}" : topic;

            lock (Subscribers)
            {
                AssertUtils.IsTrue(!Subscribers.Contains(key), "该类型重复订阅，如果需要订阅请区分不同的GroupId");
                Subscribers.Add(key);
            }

            _logger.LogInformation($"订阅[{topic}],threadcount={threadCount}");
            for (int i = 0; i < threadCount; i++)
            {
                var consumer = new RabbitMQConsumer(this._serviceProvider, this._producer);
                _consumers.Add(consumer);
                consumer.OnMessage += async (result) =>
               {
                   var obj = _options.Serializer.Deserialize<T>(result.Data);
                  return  await handler(obj);
               };
                await consumer.Subscribe(topic, groupId, cancellationToken);
            }
        }

        public void Dispose()
        {
            _producer.Dispose();

            foreach (var item in _consumers)
            {
                item.Dispose();
            }

            _delayQueueConsumer?.Dispose();

            With.NoException(_logger, () =>
            {
                _connection.Close();
            }, "关闭rabbitMQ连接");
        }

        #region private

        private void InitDelayQueue()
        {
            if (_isInitDelayQueue) return;
            lock (this)
            {
                if (_isInitDelayQueue) return;
                _isInitDelayQueue = true;
            }

            _delayQueueConsumer = new RabbitMQDelayConsumer(this._serviceProvider, this._producer);
            _delayQueueConsumer.Subscribe();

        }

        private string GetTopic(Type type)
        {
            string topicName = type.Name;

            var topicAttr = TopicAttribute.GetTopicAttribute(type);
            if (topicAttr != null && !string.IsNullOrEmpty(topicAttr.Name))
            {
                topicName = topicAttr.Name;
            }
            return $"{_options.TopicPrefix ?? ""}{topicName}";
        }


        #endregion
    }
}
