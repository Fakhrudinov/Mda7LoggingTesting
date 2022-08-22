using System;
using System.Security.Authentication;
using GreenPipes;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Restaurant.Messages.Repositories.Implementation;
using Restaurant.Messages.Repositories.Interfaces;
using Restaurant.Notification.Consumers;
using Restaurant.Notification.Models;

namespace Restaurant.Notification
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            CreateHostBuilder(args).Build().Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
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
                        x.AddConsumer<NotifyConsumer>(
                            configurator =>
                            {
                                configurator.UseScheduledRedelivery(config =>
                                {
                                    config.Interval(
                                        2, // retry count
                                        TimeSpan.FromSeconds(20));// interval
                                });
                                configurator.UseMessageRetry(config =>
                                {
                                    config.Incremental(
                                        retryLimit: 2,
                                        initialInterval: TimeSpan.FromSeconds(2),
                                        intervalIncrement: TimeSpan.FromSeconds(4));
                                });
                            })
                            .Endpoint(e =>
                            {
                                e.Temporary = true;
                            });

                        x.AddConsumer<NotifyFaultConsumer>()
                            .Endpoint(e =>
                            {
                                e.Temporary = true;
                            }); 

                        x.UsingRabbitMq((context,cfg) =>
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

                    services.AddSingleton<IInMemoryRepository<NotifyModel>, InMemoryRepository<NotifyModel>>();
                    services.AddSingleton<Notifier>();
                    services.AddMassTransitHostedService(true);
                });
    }
}