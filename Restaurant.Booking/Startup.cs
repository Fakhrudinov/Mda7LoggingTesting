using MassTransit;
using MassTransit.Audit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Prometheus;
using Restaurant.Booking.Audit;
using Restaurant.Booking.Consumers;
using Restaurant.Booking.Models;
using Restaurant.Booking.Saga;
using Restaurant.Messages.CustomExceptions;
using Restaurant.Messages.Repositories.Implementation;
using Restaurant.Messages.Repositories.Interfaces;
using System;
using System.Security.Authentication;


namespace Restaurant.Booking
{
    internal class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            IConfigurationRoot config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json").Build();
            IConfigurationSection sect = config.GetSection("HostConfig");

            bool shouldUseSSL = Boolean.Parse(sect.GetSection("ShouldUseSSL").Value);

            services.AddControllers();

            services.AddMassTransit(x =>
            {
                services.AddSingleton<IMessageAuditStore, AuditStore>();

                var serviceProvider = services.BuildServiceProvider();
                var auditStore = serviceProvider.GetService<IMessageAuditStore>();

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
                    .InMemoryRepository();

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

                    cfg.ConnectSendAuditObservers(auditStore);
                    cfg.ConnectConsumeAuditObserver(auditStore);
                });
            });

            services.AddSingleton<IInMemoryRepository<BookingRequestModel>, InMemoryRepository<BookingRequestModel>>();

            services.AddTransient<RestaurantBooking>();
            services.AddTransient<RestaurantBookingSaga>();
            services.AddSingleton<Restaurant>();

            services.AddHostedService<Worker>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapMetrics();
                endpoints.MapControllers();
            });
        }
    }
}
