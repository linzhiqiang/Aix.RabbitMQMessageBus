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


        public static string GetDelayTopic(RabbitMQMessageBusOptions options)
        {
            return $"{options.TopicPrefix }delay-queue";
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
            return $"{topic}-error-exchange";
        }

        #endregion
    }
}
