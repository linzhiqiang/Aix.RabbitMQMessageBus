using Aix.RabbitMQMessageBus.Model;
using Aix.RabbitMQMessageBus.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aix.RabbitMQMessageBus.Impl
{
    /// <summary>
    /// 延迟队列插件 实现 请安装插件
    /// </summary>
    internal class RabbitMQDelayConsumer : IRabbitMQDelayConsumer
    {
        private IServiceProvider _serviceProvider;
        private ILogger<RabbitMQDelayConsumer> _logger;
        private RabbitMQMessageBusOptions _options;
        private IRabbitMQProducer _producer;

        IConnection _connection;
        IModel _channel;

        private volatile bool _isStart = false;
        bool _autoAck = true;
        ulong _currentDeliveryTag = 0; //记录最新的消费tag，便于手工确认
        private int Count = 0;//记录消费记录数，便于手工批量确认

        private int ManualCommitBatch = 1;

        public RabbitMQDelayConsumer(IServiceProvider serviceProvider, IRabbitMQProducer producer)
        {
            _serviceProvider = serviceProvider;
            _producer = producer;

            _logger = serviceProvider.GetService<ILogger<RabbitMQDelayConsumer>>();
            _options = serviceProvider.GetService<RabbitMQMessageBusOptions>();

            _connection = _serviceProvider.GetService<IConnection>();
            _channel = _connection.CreateModel();

            _autoAck = false;// _options.AutoAck;
            ManualCommitBatch = 1;// _options.ManualCommitBatch;
        }

        private void CreateDelayExchangeAndQueue()
        {
            //var exchange = Helper.GeteDelayExchangeName(_options);
            var exchangeArguments = new Dictionary<string, object>();
            exchangeArguments.Add("x-delayed-type", "fanout");  //分发类型在这类设置 fanout direct topic
            var exchange = "my-delayed-exchange";
            //定义交换器
            _channel.ExchangeDeclare(
               exchange: exchange,
               type: ExchangeType.Fanout,
               durable: true,
                autoDelete: false,
                arguments: exchangeArguments
               );

            var delayTopic = Helper.GetDelayTopic(_options);
            var routingKey = Helper.GeteRoutingKey(delayTopic, "");
            var queue = delayTopic;

           
            //定义队列
            _channel.QueueDeclare(queue: queue,
                                     durable: true,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

            //绑定交换器到队列
            _channel.QueueBind(queue, exchange, routingKey);
        }

        public Task Subscribe()
        {
            _isStart = true;
            var exchange = Helper.GeteDelayExchangeName(_options);
            var exchangeArguments = new Dictionary<string, object>();
            exchangeArguments.Add("x-delayed-type", "fanout");  //分发类型在这类设置 fanout direct topic
            //定义交换器
            _channel.ExchangeDeclare(
               exchange: exchange,
               type: "x-delayed-message",//延迟队列 这里写死 x-delayed-message
               durable: true,
                autoDelete: false,
                arguments: exchangeArguments
               );

            var delayTopic = Helper.GetDelayTopic(_options);
            var routingKey = Helper.GeteRoutingKey(delayTopic, "");
            var queue = delayTopic;


            //定义队列
            _channel.QueueDeclare(queue: queue,
                                     durable: true,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

            //绑定交换器到队列
            _channel.QueueBind(queue, exchange, routingKey);

            //消费死信队列的任务
            var prefetchCount = _options.PrefetchCount ; // (ushort)ManualCommitBatch;// _options.ManualCommitBatch;  //最大值：ushort.MaxValue
            _channel.BasicQos(0, prefetchCount, false); //客户端最多保留这么多条未确认的消息 只有autoack=false 有用
            var consumer = new AsyncEventingBasicConsumer(_channel);// EventingBasicConsumer
            consumer.Received += Received;
            consumer.Shutdown += Consumer_Shutdown;
            _logger.LogInformation("开始消费延迟任务数据...");
            String consumerTag = _channel.BasicConsume(queue, _autoAck, queue, consumer);
            return Task.CompletedTask;
        }

        private async Task Received(object sender, BasicDeliverEventArgs deliverEventArgs)
        {
            if (!_isStart) return; //这里有必要的，关闭时已经手工提交了，由于客户端还有累计消息会继续执行，但是不能确认（连接已关闭）
            try
            {
                await Handler(deliverEventArgs.Body.ToArray());
            }
            catch (Exception ex)
            {
                _logger.LogError($"rabbitMQ消费延迟消息失败, {ex.Message}, {ex.StackTrace}");
            }
            finally
            {
                _currentDeliveryTag = deliverEventArgs.DeliveryTag;// _currentDeliveryTag = deliverEventArgs.DeliveryTag;//放在消费后，防止未处理完成但是关闭时也确认了该消息
                Count++;
                ManualAck(false);
            }
        }

        /// <summary>
        /// 延迟任务消费处理   三种情况： 1=任务到期进去即时任务，2=任务没有到期 继续进入延迟队列。3=任务到期，是失败重试的任务需要进入单个队列
        /// </summary>
        /// <param name="data"></param>
        private Task Handler(byte[] data)
        {
            var delayMessage = _options.Serializer.Deserialize<RabbitMessageBusData>(data);

            var delayTime = TimeSpan.FromMilliseconds(delayMessage.ExecuteTimeStamp - DateUtils.GetTimeStamp(DateTime.Now));
            if (delayTime > TimeSpan.Zero)
            {//继续延迟
                _producer.ProduceDelayAsync(delayMessage.Type, data, delayTime);
            }
            else
            {//即时任务
                if (delayMessage.ErrorCount <= 0)
                {
                    _producer.ProduceAsync(delayMessage.Type, data);
                }
                else //由于错误，需要重试的任务
                {
                    _producer.ErrorReProduceAsync(delayMessage.Type, delayMessage.ErrorGroupId, data);
                }
            }
            return Task.CompletedTask;
        }

        private Task Consumer_Shutdown(object sender, ShutdownEventArgs e)
        {
            _logger.LogInformation($"RabbitMQ关闭延迟消费者，reason:{e.ReplyText}");
            return Task.CompletedTask;
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
                    // _logger.LogInformation("关闭时确认剩余的未确认消息"+ _currentDeliveryTag);
                    With.NoException(() => { _channel.BasicAck(_currentDeliveryTag, true); });
                    Count = 0;
                }
            }
            else //按照批量确认
            {
                //if (Count % _options.ManualCommitBatch == 0)
                if (Count % ManualCommitBatch == 0)
                {
                    With.NoException(() => { _channel.BasicAck(_currentDeliveryTag, true); });
                    Count = 0;
                }
            }
        }

        public void Close()
        {
            _isStart = false;
            _logger.LogInformation($"RabbitMQ开始关闭延迟消费者......");
            With.NoException(_logger, () =>
            {
                ManualAck(true);
            }, "关闭延迟消费者前手工确认最后未确认的消息");
            With.NoException(_logger, () => { this._channel?.Close(); }, "关闭延迟消费者");
        }

        public void Dispose()
        {
            Close();
        }
    }
}
