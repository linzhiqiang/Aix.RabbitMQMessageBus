using Aix.RabbitMQMessageBus.Model;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Aix.RabbitMQMessageBus
{
    /// <summary>
    /// 消息发布订阅接口 
    /// </summary>
    public interface IRabbitMQMessageBus : IDisposable
    {
        /// <summary>
        /// 发布消息
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        Task PublishAsync(Type messageType, object message);

        /// <summary>
        /// 发布延迟消息 kafka未实现,rabbitmq已实现,redis已实现
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="message"></param>
        /// <param name="delay"></param>
        /// <returns></returns>
        Task PublishDelayAsync(Type messageType, object message, TimeSpan delay);

        /// <summary>
        /// 订阅消息 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handler"></param>
        /// <param name="context"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task SubscribeAsync<T>(Func<T, Task> handler, SubscribeOptions subscribeOptions = null, CancellationToken cancellationToken = default(CancellationToken)) where T : class;

    }
}
