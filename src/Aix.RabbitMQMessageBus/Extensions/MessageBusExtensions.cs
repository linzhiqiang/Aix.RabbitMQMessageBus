using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Aix.RabbitMQMessageBus
{
    public static class MessageBusExtensions
    {
        public static Task PublishAsync<T>(this IRabbitMQMessageBus messageBus, T message)
        {
            return messageBus.PublishAsync(typeof(T), message);
        }

        public static Task PublishDelayAsync<T>(this IRabbitMQMessageBus messageBus, T message, TimeSpan delay)
        {
            return messageBus.PublishDelayAsync(typeof(T), message, delay);
        }
    }
}
