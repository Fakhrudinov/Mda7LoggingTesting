using Microsoft.Extensions.Logging;
using System;

namespace Restaurant.Notification
{
    public class Notifier
    {
        private readonly ILogger _logger;

        public Notifier(ILogger<Notifier> logger)
        {
            _logger = logger;
        }

        public void Notify(Guid orderId, Guid clientId, string message)
        {
            _logger.LogInformation($"Notification-Notifier=Заказ#{orderId}, клиент {clientId}. Сообщение: {message}");
            Console.WriteLine($"Notification-Notifier=Заказ#{orderId}, клиент {clientId}. Сообщение: {message}");
        }
    }
}