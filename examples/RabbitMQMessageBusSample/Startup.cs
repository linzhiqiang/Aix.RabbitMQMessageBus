using Aix.RabbitMQMessageBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQMessageBusSample.HostedService;

namespace RabbitMQMessageBusSample
{
    public class Startup
    {
        internal static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            var options = CmdOptions.Options;
            services.AddSingleton(options);

            var rabbitMQMessageBusOptions = context.Configuration.GetSection("rabbitmq").Get<RabbitMQMessageBusOptions>();
            services.AddRabbitMQMessageBus(rabbitMQMessageBusOptions);


            if ((options.Mode & (int)ClientMode.Consumer) > 0)
            {
                services.AddHostedService<MessageBusConsumeService>();
            }
            if ((options.Mode & (int)ClientMode.Producer) > 0)
            {
                services.AddHostedService<MessageBusProduerService>();
            }
        }
    }
}
