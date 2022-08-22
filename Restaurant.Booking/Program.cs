using System;
using System.Security.Authentication;
using GreenPipes;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Restaurant.Booking.Consumers;
using Restaurant.Booking.Models;
using Restaurant.Booking.Saga;
using Restaurant.Messages.CustomExceptions;
using Restaurant.Messages.Repositories.Implementation;
using Restaurant.Messages.Repositories.Interfaces;

namespace Restaurant.Booking
{
    public static class Program
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
                        x.AddConsumer<BookingRequestConsumer>(
                            configurator =>
                            {
                                configurator.UseScheduledRedelivery(config =>
                                {
                                    config.Intervals(
                                        TimeSpan.FromSeconds(10),
                                        TimeSpan.FromSeconds(20),
                                        TimeSpan.FromSeconds(30));
                                });
                                configurator.UseMessageRetry(config =>
                                {
                                    config.Incremental(
                                        retryLimit: 3, 
                                        initialInterval: TimeSpan.FromSeconds(1),
                                        intervalIncrement: TimeSpan.FromSeconds(2));
                                    config.Handle<BookingException>();
                                });
                            })
                            .Endpoint(e =>
                            {
                                e.Temporary = true;
                            });

                        x.AddConsumer<BookingCancelRequested>()
                            .Endpoint(e =>
                            {
                                e.Temporary = true;
                            });

                        x.AddConsumer<BookingRequestFaultConsumer>()
                            .Endpoint(e =>
                            {
                                e.Temporary = true;
                            });

                        x.AddSagaStateMachine<RestaurantBookingSaga, RestaurantBooking>()
                            .Endpoint(e => e.Temporary = true)
                            .InMemoryRepository();

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
                    services.AddMassTransitHostedService(true);

                    services.AddSingleton<IInMemoryRepository<BookingRequestModel>, InMemoryRepository<BookingRequestModel>>();

                    services.AddTransient<RestaurantBooking>();
                    services.AddTransient<RestaurantBookingSaga>();
                    services.AddSingleton<Restaurant>();

                    services.AddHostedService<Worker>();
                });
    }
}