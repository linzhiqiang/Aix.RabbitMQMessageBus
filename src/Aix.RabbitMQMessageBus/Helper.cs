using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Aix.RabbitMQMessageBus
{
    internal static class Helper
    {
        public static string GeteExchangeName(string topic)
        {
            return $"{topic}-exchange";
        }
        public static string GeteQueueName(string topic, string groupId)
        {
            if (string.IsNullOrEmpty(groupId))
            {
                return $"{topic}-queue";
            }
            else
            {
                return $"{topic}-{groupId}-queue";
            }
        }

        public static string GeteRoutingKey(string topic, string groupId)
        {
            if (string.IsNullOrEmpty(groupId))
            {
                return $"{topic}-routingkey";
            }
            else
            {
                return $"{topic}-{groupId}-routingkey";
            }

        }

        #region delay

        public static string GeteDelayExchangeName(RabbitMQMessageBusOptions options)
        {
            return $"{options.TopicPrefix }delay-exchange";
        }

        public static string GetDelayConsumerExchange(RabbitMQMessageBusOptions options)
        {
            return $"{options.TopicPrefix }delay-consumer-exchange";
        }

        public static string GetDelayConsumerQueue(RabbitMQMessageBusOptions options)
        {
            return $"{options.TopicPrefix }delay-consumer-queue";
        }

        public static List<string> GetDelayTopicList(RabbitMQMessageBusOptions options)
        {
            List<string> delayTopics = new List<string>();
            foreach (var item in options.GetDelayQueueConfig())
            {
                var temp = GetDelayTopic(options, item.Value);
                delayTopics.Add(temp);
            }

            return delayTopics;
        }
        public static string GetDelayTopic(RabbitMQMessageBusOptions options, TimeSpan delay)
        {
            var dealySecond = (int)delay.TotalSeconds;

            var keys = options.GetDelayQueueConfig().Keys.ToList();

            //for (int i = 0; i < keys.Count; i++)
            for (int i = keys.Count-1; i >=0; i--)
            {
                if (dealySecond > keys[i])
                {
                    return GetDelayTopic(options, options.GetDelayQueueConfig()[keys[i]]);
                }
            }

            return GetDelayTopic(options, options.GetDelayQueueConfig()[keys[0]]);

        }

        public static string GetDelayTopic(RabbitMQMessageBusOptions options, string postfix)
        {
            return $"{options.TopicPrefix }delay-queue{postfix}";
        }

        #endregion

        #region  error

        /// <summary>
        /// 失败重试 进入单个交换器 
        /// </summary>
        /// <param name="topic"></param>
        /// <returns></returns>
        public static string GetErrorReEnqueneExchangeName(string topic)
        {
            return $"{topic}-reenqueue-exchange";
        }

        #endregion
    }
}
