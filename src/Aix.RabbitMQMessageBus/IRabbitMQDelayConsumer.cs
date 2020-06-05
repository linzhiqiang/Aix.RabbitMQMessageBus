using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Aix.RabbitMQMessageBus
{
    internal interface IRabbitMQDelayConsumer : IDisposable
    {
        Task Subscribe();

        void Close();
    }
}
