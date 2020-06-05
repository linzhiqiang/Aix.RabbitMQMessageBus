using System;
using System.Collections.Generic;
using System.Text;

namespace Aix.RabbitMQMessageBus.Serializer
{
    public interface ISerializer
    {
        byte[] Serialize<T>(T data);

        T Deserialize<T>(byte[] bytes);
    }
}
