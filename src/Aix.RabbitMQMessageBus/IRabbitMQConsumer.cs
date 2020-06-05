
using Aix.RabbitMQMessageBus.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Aix.RabbitMQMessageBus
{
    internal interface IRabbitMQConsumer : IDisposable
    {
        Task Subscribe(string topic, string groupId, CancellationToken cancellationToken);

        event Func<RabbitMessageBusData, Task> OnMessage;
        void Close();
    }
}
