using Aix.RabbitMQMessageBus.Model;
using Aix.RabbitMQMessageBus.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Aix.RabbitMQMessageBus.Impl
{
    internal class RabbitMQConsumer : IRabbitMQConsumer
    {
        private IServiceProvider _serviceProvider;
        private ILogger<RabbitMQConsumer> _logger;
        private RabbitMQMessageBusOptions _options;
        IRabbitMQProducer _producer;

        IConnection _connection;
        IModel _channel;
        private volatile bool _isStart = false;
        bool _autoAck = true;
        ulong _currentDeliveryTag = 0; //记录最新的消费tag，便于手工确认
        private int Count = 0;//记录消费记录数，便于手工批量确认

        private string _groupId = "";

        public RabbitMQConsumer(IServiceProvider serviceProvider, IRabbitMQProducer producer)
        {
            _serviceProvider = serviceProvider;
            _producer = producer;

            _logger = serviceProvider.GetService<ILogger<RabbitMQConsumer>>();
            _options = serviceProvider.GetService<RabbitMQMessageBusOptions>();

            _connection = _serviceProvider.GetService<IConnection>();
            _channel = _connection.CreateModel();
            _channel.ModelShutdown += _channel_ModelShutdown;
            _autoAck = _options.AutoAck;
        }

        private Task Consumer_ConsumerCancelled(object sender, ConsumerEventArgs @event)
        {
            // _logger.LogInformation($"RabbitMQ ConsumerCancelled");
            return Task.CompletedTask;
        }

        private void _channel_ModelShutdown(object sender, ShutdownEventArgs e)
        {
            //_logger.LogInformation($"RabbitMQ channel  shutdown，reason:{e.ReplyText}");
        }

        private Task Consumer_Shutdown(object sender, ShutdownEventArgs e)
        {
            _logger.LogInformation($"RabbitMQ关闭消费者，reason:{e.ReplyText}");
            return Task.CompletedTask;
        }

        public event Func<RabbitMessageBusData, Task<bool>> OnMessage;

        public Task Subscribe(string topic, string groupId, CancellationToken cancellationToken)
        {
            _isStart = true;
            _groupId = groupId ?? "";
            var exchange = Helper.GeteExchangeName(topic);
            var routingKey = Helper.GeteRoutingKey(topic, _groupId);
            var queue = Helper.GeteQueueName(topic, _groupId);

            //定义交换器
            _channel.ExchangeDeclare(
               exchange: exchange,
               type: ExchangeType.Fanout,//所有绑定的队列都发送
               durable: true,
                autoDelete: false,
                arguments: null
               );

            //定义一个失败重试入口的交换器  重试任务交换器
            var errorReEnqueneExchangeName = Helper.GetErrorReEnqueneExchangeName(topic);
            _channel.ExchangeDeclare(
               exchange: errorReEnqueneExchangeName,
               type: ExchangeType.Direct,//根据routeingkey分发到不同队列
               durable: true,
                autoDelete: false,
                arguments: null
               );


            //定义队列
            _channel.QueueDeclare(queue: queue,
                                     durable: true,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

            //绑定交换器到队列
            _channel.QueueBind(queue, exchange, routingKey);
            //绑定队列到重试交换器上
            _channel.QueueBind(queue, errorReEnqueneExchangeName, routingKey);

            var prefetchCount =  _options.ManualCommitBatch;  //最大值：ushort.MaxValue
            prefetchCount =(ushort) (prefetchCount > 100 ? prefetchCount : 100);
            //参数名含义prefetchSize批量取的消息的总大小，0为不限制prefetchCount消费完prefetchCount条（prefetchCount条消息被ack）才再次推送globalglobal为true表示对channel进行限制，否则对每个消费者进行限制，因为一个channel允许有多个消费者
            _channel.BasicQos(0, prefetchCount, false); //客户端最多保留这么多条未确认的消息 只有autoack=false 有用
            var consumer = new AsyncEventingBasicConsumer(_channel);//EventingBasicConsumer 同步

            _logger.LogInformation("开始消费数据...");
            consumer.ConsumerCancelled += Consumer_ConsumerCancelled;
            consumer.Received += Consumer_Received;
            consumer.Shutdown += Consumer_Shutdown;

            String consumerTag = _channel.BasicConsume(queue, _autoAck, topic, consumer);
            return Task.CompletedTask;
        }

      
        private async Task Consumer_Received(object sender, BasicDeliverEventArgs deliverEventArgs)
        {
            if (!_isStart) return; //这里有必要的，关闭时已经手工提交了，由于客户端还有累计消息会继续执行，但是不能确认（连接已关闭）
            try
            {
                var data = _options.Serializer.Deserialize<RabbitMessageBusData>(deliverEventArgs.Body.ToArray());
                var isSuccess = await Handler(data);
                if (isSuccess == false)
                {
                    ExecuteErrorToDelayTask(data);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,$"rabbitMQ消费接收消息失败");
            }
            finally
            {
                _currentDeliveryTag = deliverEventArgs.DeliveryTag;// _currentDeliveryTag = deliverEventArgs.DeliveryTag;//放在消费后，防止未处理完成但是关闭时也确认了该消息
                Count++;
                ManualAck(false);
            }
        }

        /// <summary>
        /// 加入延迟队列重试
        /// </summary>
        /// <param name="data"></param>
        private void ExecuteErrorToDelayTask(RabbitMessageBusData data)
        {
            if (data.ErrorCount < _options.MaxErrorReTryCount)
            {
                var delay = TimeSpan.FromSeconds(GetDelaySecond(data.ErrorCount));
                data.ErrorCount++;
                data.ErrorGroupId = _groupId;
                data.ExecuteTimeStamp = DateUtils.GetTimeStamp(DateTime.Now.Add(delay));

                var delayData = _options.Serializer.Serialize(data);

                if (delay > TimeSpan.Zero)
                {
                    _producer.ProduceDelayAsync(data.Type, delayData, delay);
                }
                else //立即重试的情况
                {
                    _producer.ErrorReProduceAsync(data.Type, data.ErrorGroupId, delayData);
                }
            }
        }

        private int GetDelaySecond(int errorCount)
        {
            var retryStrategy = _options.GetRetryStrategy();
            if (errorCount < retryStrategy.Length)
            {
                return retryStrategy[errorCount];
            }
            return retryStrategy[retryStrategy.Length - 1];
        }

        private void ManualAck(bool isForce)
        {
            if (_autoAck) return;
            //单条确认
            // _channel.BasicAck(deliverEventArgs.DeliveryTag, false); //可以优化成批量提交 如没10条提交一次 true，最后关闭时记得也要提交最后一次的消费

            //批量确认
            if (isForce) //关闭时强制确认剩余的
            {
                if (Count > 0)
                {
                    _logger.LogInformation($"关闭时确认剩余的未确认消息{_currentDeliveryTag},{Count}");
                    With.NoException(() => { _channel.BasicAck(_currentDeliveryTag, true); });
                    Count = 0;
                }
            }
            else //按照批量确认
            {
                if (Count % _options.ManualCommitBatch == 0)
                {
                    With.NoException(() => { _channel.BasicAck(_currentDeliveryTag, true); });
                    Count = 0;
                }
            }
        }

        /// <summary>
        /// 执行消费事件
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private async Task<bool> Handler(RabbitMessageBusData obj)
        {
            var isSuccess = true; //失败标识需要重试
            if (OnMessage == null) return isSuccess;

            try
            {
                isSuccess = await OnMessage(obj);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"rabbitMQ消费失败, topic={obj.Type}，group={_groupId}");
                isSuccess = false;
            }
            return isSuccess;
        }

        public void Close()
        {
            _isStart = false;
            _logger.LogInformation($"RabbitMQ开始关闭消费者......");
            With.NoException(_logger, () =>
            {
                ManualAck(true);
            }, "关闭消费者前手工确认最后未确认的消息");
            With.NoException(_logger, () => { this._channel?.Close(); }, "关闭消费者");
        }

        public void Dispose()
        {
            Close();
        }
    }
}
