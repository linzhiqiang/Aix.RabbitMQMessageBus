using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace Aix.RabbitMQMessageBus.Foundation
{
    public class ChannelScope : IDisposable
    {
        ObjectPool<ChannelScope> _objectPool;
        public IModel Channel { get; }
        public ChannelScope(ObjectPool<ChannelScope> objectPool, IModel channel)
        {
            _objectPool = objectPool;
            Channel = channel;
        }
        public void Dispose()
        {
            _objectPool.ReturnObject(this);
        }
    }
}
