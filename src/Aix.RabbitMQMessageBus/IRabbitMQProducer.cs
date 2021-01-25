using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Aix.RabbitMQMessageBus
{
    internal interface IRabbitMQProducer : IDisposable
    {
        /// <summary>
        /// 任务插入即时队列
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        bool ProduceAsync(string topic, byte[] data);

        /// <summary>
        /// 延迟任务 插入延迟队列
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="data"></param>
        /// <param name="delay"></param>
        /// <returns></returns>
        bool ProduceDelayAsync(string topic, byte[] data,TimeSpan delay);

        /// <summary>
        /// 重试的任务，到期后插入即时队列
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="groupId"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        bool ErrorReProduceAsync(string topic, string groupId, byte[] data);
    }
}
