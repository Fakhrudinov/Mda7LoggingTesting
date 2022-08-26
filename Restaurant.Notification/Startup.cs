using MassTransit;
using MassTransit.Audit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Prometheus;
using Restaurant.Messages.Repositories.Implementation;
using Restaurant.Messages.Repositories.Interfaces;
using Restaurant.Notification.Audit;
using Restaurant.Notification.Consumers;
using System;
using System.Security.Authentication;
using GreenPipes;
using Restaurant.Notification.Models;

namespace Restaurant.Notification
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

            services.AddSingleton<IInMemoryRepository<NotifyModel>, InMemoryRepository<NotifyModel>>();
            services.AddSingleton<Notifier>();

            services.Configure<MassTransitHostOptions>(options =>
            {
                options.WaitUntilStarted = true;
                options.StartTimeout = TimeSpan.FromSeconds(30);
                options.StopTimeout = TimeSpan.FromMinutes(1);
            });
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
