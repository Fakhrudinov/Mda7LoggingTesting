using MassTransit;
using MassTransit.Audit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Prometheus;
using Repositories.Interfaces;
using Repositories.Models;
using Repositoties;
using Restaurant.Kitchen.Audit;
using Restaurant.Kitchen.Consumers;
using Restaurant.Kitchen.Models;
using Restaurant.Messages.CustomExceptions;
using System;
using System.Security.Authentication;


namespace Restaurant.Kitchen
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

                    cfg.ConnectSendAuditObservers(auditStore);
                    cfg.ConnectConsumeAuditObserver(auditStore);
                });
            });

            RepositoryConnectionSettings.ConnectionString = config.GetSection("RepositoryConnectionSettings").GetSection("ConnectionString").Value;
            Repositories.PrepareDataBases.CreateNewKitchenTable();
            services.AddSingleton<IDataBaseRepositoty<TableBookedModel>, KitchenIdempotencytRepository>();

            services.AddSingleton<Manager>();
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
