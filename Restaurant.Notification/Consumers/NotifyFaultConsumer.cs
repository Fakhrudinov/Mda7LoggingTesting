using MassTransit;
using Microsoft.Extensions.Logging;
using Restaurant.Messages.Interfaces;
using System.Threading.Tasks;

namespace Restaurant.Notification.Consumers
{
    public class NotifyFaultConsumer : IConsumer<Fault<INotify>>
    {
        private readonly ILogger _logger;

        public NotifyFaultConsumer(ILogger<NotifyFaultConsumer> logger)
        {
            _logger = logger;
        }
        /// <summary>
        /// Подписка на событие ошибки в доставке уведомления
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public Task Consume(ConsumeContext<Fault<INotify>> context)
        {
            _logger.LogWarning($"NotifyFaultConsumer Event for {context.Message.Message.OrderId}");
            return Task.CompletedTask;
        }
    }
}
