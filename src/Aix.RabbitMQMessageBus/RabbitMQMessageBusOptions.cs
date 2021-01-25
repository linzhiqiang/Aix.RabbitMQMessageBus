using Aix.RabbitMQMessageBus.Serializer;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Aix.RabbitMQMessageBus
{
    public class RabbitMQMessageBusOptions
    {
        private int[] DefaultRetryStrategy = new int[] { 1, 5, 10, 30, 60, 2 * 60, 2 * 60, 2 * 60, 5 * 60, 5 * 60 };
        /// <summary>
        /// 默认延迟队列 延迟时间配置  （秒，延迟队列名称后缀）
        /// </summary>
        private static Dictionary<int, string> DefaultDelayQueueConfig = new Dictionary<int, string>
        {
             { (int)TimeSpan.FromSeconds(1).TotalSeconds,"1s"},
             { (int)TimeSpan.FromSeconds(2).TotalSeconds,"2s"},
             { (int)TimeSpan.FromSeconds(3).TotalSeconds,"3s"},
             { (int)TimeSpan.FromSeconds(4).TotalSeconds,"4s"},
            { (int)TimeSpan.FromSeconds(5).TotalSeconds,"5s"},
            { (int)TimeSpan.FromSeconds(30).TotalSeconds,"30s"},
            { (int)TimeSpan.FromMinutes(1).TotalSeconds,"1m"},
            { (int)TimeSpan.FromMinutes(10).TotalSeconds,"10m"},
            { (int)TimeSpan.FromMinutes(30).TotalSeconds,"30m"},
            { (int)TimeSpan.FromHours(1).TotalSeconds,"1h"},
            { (int)TimeSpan.FromDays(1).TotalSeconds,"1d"},
        };
        public RabbitMQMessageBusOptions()
        {
            //Port = 5672;
            //VirtualHost = "/";
            this.TopicPrefix = "aix-";
            this.Serializer = new MessagePackSerializer();
        }
        public string HostName { get; set; }

        public int Port { get; set; } = 5672;

        public string VirtualHost { get; set; } = "/";

        public string UserName { get; set; }

        public string Password { get; set; }

        /***********************************************************/

        /// <summary>
        /// topic前缀，为了防止重复，建议用项目名称
        /// </summary>
        public string TopicPrefix { get; set; }

        /// <summary>
        /// 自定义序列化，默认为MessagePack
        /// </summary>
        public ISerializer Serializer { get; set; }

        /// <summary>
        /// 发布消息时是否确认消息收到确认 false=不确认 true=确认，默认false
        /// </summary>
        public bool ConfirmSelect { get; set; } = false;

        /// <summary>
        /// 默认每个类型的消费线程数 默认4个
        /// </summary>
        public int DefaultConsumerThreadCount { get; set; } = 2;

        /// <summary>
        /// 消费时是否自动确认
        /// </summary>
        public bool AutoAck { get; set; } = true;

        /// <summary>
        /// AutoAck=false时每多少个消息提交一次 默认10条消息提交一次
        /// </summary>
        public ushort ManualCommitBatch { get; set; } = 1;

        public Dictionary<int, string> DelayQueueConfig { get; set; }


        public Dictionary<int, string> GetDelayQueueConfig()
        {
            if (DelayQueueConfig == null || DelayQueueConfig.Count == 0) return DefaultDelayQueueConfig;
            return DelayQueueConfig;
        }

        /// <summary>
        /// 最大错误重试次数 默认10次
        /// </summary>
        public int MaxErrorReTryCount { get; set; } = 10;

        /// <summary>
        /// 失败重试延迟策略 单位：秒  默认失败次数对应值延迟时间[ 1,5, 10, 30,  60,  60, 2 * 60, 2 * 60, 5 * 60, 5 * 60  ];
        /// </summary>
        public int[] RetryStrategy { get; set; }

        public int[] GetRetryStrategy()
        {
            if (RetryStrategy == null || RetryStrategy.Length == 0) return DefaultRetryStrategy;
            return RetryStrategy;
        }

    }

}
