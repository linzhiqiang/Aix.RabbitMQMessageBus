using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Aix.RabbitMQMessageBus.Foundation
{
    public class ObjectPool<T>
    {
        BlockingCollection<T> BlockQueue = new BlockingCollection<T>(new ConcurrentQueue<T>());

        public void AddObject(T obj)
        {
            BlockQueue.Add(obj);
        }

        public void ReturnObject(T obj)
        {
            BlockQueue.Add(obj);
        }

        public T BorrowObject(CancellationToken cancellationToken = default(CancellationToken))
        {
            return BlockQueue.Take(cancellationToken);
        }


    }
}
