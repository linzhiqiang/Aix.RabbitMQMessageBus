using CommandLine;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;

namespace RabbitMQMessageBusSample
{
    /*
     // m=1生产者 m=2消费者 m=3生产+消费      q=生产数量
     dotnet run -m 1 -q 10      //生产
      dotnet run -m 2    //消费
     */
    class Program
    {
        public static void Main(string[] args)
        {
            Parser parser = new Parser((setting) =>
            {
                setting.CaseSensitive = false;
            });
            parser.ParseArguments<CmdOptions>(args).WithParsed((options) =>
            {
                CmdOptions.Options = options;
                CreateHostBuilder(args, options).Build().Run();
            });

        }

        public static IHostBuilder CreateHostBuilder(string[] args, CmdOptions options)
        {
            return Host.CreateDefaultBuilder(args)
            .ConfigureHostConfiguration(configurationBuilder =>
            {

            })
           .ConfigureAppConfiguration((hostBulderContext, configurationBuilder) =>
           {
           })
            .ConfigureLogging((hostBulderContext, loggingBuilder) =>
            {
                loggingBuilder.SetMinimumLevel(LogLevel.Information);
                //系统也默认加载了默认的log
                loggingBuilder.ClearProviders();
                loggingBuilder.AddConsole();
            })
            .ConfigureServices(Startup.ConfigureServices);

        }

    }
}
