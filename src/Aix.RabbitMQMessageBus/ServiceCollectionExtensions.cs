using Aix.RabbitMQMessageBus;
using Aix.RabbitMQMessageBus.Utils;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace Aix.RabbitMQMessageBus
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRabbitMQMessageBus(this IServiceCollection services, RabbitMQMessageBusOptions options)
        {
            ValidOptions(options);
            var connection = CreateConnection(options);
            services
               .AddSingleton<RabbitMQMessageBusOptions>(options)
               .AddSingleton(connection)
               .AddSingleton<IRabbitMQMessageBus, RabbitMQMessageBus>();

            return services;
        }

        private static IConnection CreateConnection(RabbitMQMessageBusOptions options)
        {
            if (string.IsNullOrEmpty(options.HostName)) throw new Exception("请配置rabbitMQ的HostName参数");

            var factory = new ConnectionFactory()
            {
                //HostName = options.HostName,
                Port = options.Port,
                VirtualHost = options.VirtualHost,
                UserName = options.UserName,
                Password = options.Password,

                AutomaticRecoveryEnabled = true,
                // Protocol = Protocols.DefaultProtocol
                DispatchConsumersAsync = true
            };
            var hostNames = options.HostName.Replace(" ", "").Split(new char[] { ',', '，' }, StringSplitOptions.RemoveEmptyEntries);
            var connection = factory.CreateConnection(hostNames);
            connection.CallbackException += Connection_CallbackException;
            return connection;
        }

        private static void Connection_CallbackException(object sender, global::RabbitMQ.Client.Events.CallbackExceptionEventArgs e)
        {

        }

        private static void ValidOptions(RabbitMQMessageBusOptions options)
        {
            if (options.AutoAck == false)
            {
                AssertUtils.IsTrue(options.ManualCommitBatch >=1, "ManualCommitBatch大于等于1");
            }
            AssertUtils.IsTrue(options.DefaultConsumerThreadCount >=1, "DefaultConsumerThreadCount大于等于1");
        }
    }
}
