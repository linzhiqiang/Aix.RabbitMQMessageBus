using System;
using System.Collections.Generic;
using System.Text;

namespace Aix.RabbitMQMessageBus.Model
{
    public class RabbitMessageBusData
    {
        public string Type { get; set; }
        public byte[] Data { get; set; }

        /// <summary>
        /// 执行时间  （延迟队列有用），即时任务就是当前时间戳 
        /// </summary>
        public long ExecuteTimeStamp { get; set; }

        public int ErrorCount { get; set; }

        public string ErrorGroupId { get; set; }
    }
}
