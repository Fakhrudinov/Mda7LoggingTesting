using System;
using System.Security.Authentication;
using GreenPipes;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Repositories.Interfaces;
using Repositories.Models;
using Repositoties;
using Restaurant.Kitchen.Consumers;
using Restaurant.Kitchen.Models;
using Restaurant.Messages.CustomExceptions;

namespace Restaurant.Kitchen
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    IConfigurationRoot config = new ConfigurationBuilder()
                        .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                        .AddJsonFile("appsettings.json").Build();
                    IConfigurationSection sect = config.GetSection("HostConfig");

                    bool shouldUseSSL = Boolean.Parse(sect.GetSection("ShouldUseSSL").Value);

                    services.AddMassTransit(x =>
                    {
                        x.AddConsumer<KitchenRequestedConsumer>(
                            configurator =>
                            {
                                configurator.UseScheduledRedelivery(config =>
                                {
                                    config.Interval(
                                        1,
                                        TimeSpan.FromSeconds(5));
                                });
                                configurator.UseMessageRetry(config =>
                                {
                                    config.Incremental(
                                        retryLimit: 1,
                                        initialInterval: TimeSpan.FromSeconds(10),
                                        intervalIncrement: TimeSpan.FromSeconds(30));
                                    config.Handle<KitchenException>();
                                });
                            }
                            )
                            .Endpoint(e =>
                            {
                                e.Temporary = true;
                            });
                        
                        x.AddConsumer<KitchenFaultConsumer>()
                            .Endpoint(e =>
                            {
                                e.Temporary = true;
                            }); 

                        x.UsingRabbitMq((context, cfg) =>
                        {
                            cfg.Host(
                                sect.GetSection("HostName").Value,
                                ushort.Parse(sect.GetSection("Port").Value),
                                sect.GetSection("VirtualHost").Value,
                                h =>
                                {
                                    if (shouldUseSSL)
                                    {
                                        h.UseSsl(s =>
                                        {
                                            s.Protocol = SslProtocols.Tls12;
                                        });
                                    }
                                    h.Username(sect.GetSection("UserName").Value);
                                    h.Password(sect.GetSection("Password").Value);
                                });

                            cfg.UseMessageRetry(r =>
                            {
                                r.Exponential(5,
                                    TimeSpan.FromSeconds(1),
                                    TimeSpan.FromSeconds(100),
                                    TimeSpan.FromSeconds(5));
                                r.Ignore<StackOverflowException>();
                                r.Ignore<ArgumentNullException>(x => x.Message.Contains("Consumer"));
                            });

                            cfg.UseDelayedMessageScheduler();
                            cfg.UseInMemoryOutbox();
                            cfg.ConfigureEndpoints(context);
                        });
                    });

                    RepositoryConnectionSettings.ConnectionString = config.GetSection("RepositoryConnectionSettings").GetSection("ConnectionString").Value;
                    Repositories.PrepareDataBases.CreateNewKitchenTable();
                    services.AddSingleton<IDataBaseRepositoty<TableBookedModel>, KitchenIdempotencytRepository>();

                    services.AddSingleton<Manager>();
                    
                    services.AddMassTransitHostedService(true);
                });
    }
}