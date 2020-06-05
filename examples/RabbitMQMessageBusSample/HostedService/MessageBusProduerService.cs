using Aix.RabbitMQMessageBus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMQMessageBusSample.HostedService
{
    public class MessageBusProduerService : IHostedService
    {
        private ILogger<MessageBusProduerService> _logger;
        public IMessageBus _messageBus;
        private CmdOptions _cmdOptions;
        public MessageBusProduerService(ILogger<MessageBusProduerService> logger, IMessageBus messageBus, CmdOptions cmdOptions)
        {
            _logger = logger;
            _messageBus = messageBus;
            _cmdOptions = cmdOptions;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Task.Run(async () =>
            {
                List<Task> taskList = new List<Task>();

                taskList.Add(Producer(cancellationToken));

                await Task.WhenAll(taskList.ToArray());
            });

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("StopAsync");
            return Task.CompletedTask;
        }

        private async Task Producer(CancellationToken cancellationToken)
        {
            int producerCount = _cmdOptions.Count > 0 ? _cmdOptions.Count : 1;
            try
            {
                for (int i = 0; i < producerCount; i++)
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    try
                    {
                        var messageData = new BusinessMessage
                        {
                            MessageId = i.ToString(),
                            Content = $"我是内容_{i}",
                            CreateTime = DateTime.Now
                        };
                        await _messageBus.PublishAsync(messageData);
                        await _messageBus.PublishDelayAsync(messageData,TimeSpan.FromSeconds(8));
                        _logger.LogInformation($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss fff")}生产数据：MessageId={messageData.MessageId}");

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "生产消息出错");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "");
            }
        }
    }
}
