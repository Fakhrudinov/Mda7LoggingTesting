using System;

namespace Restaurant.Notification
{
    public class Notifier
    {
        public void Notify(Guid orderId, Guid clientId, string message)
        {
            Console.WriteLine($"Notification-Notifier=Заказ#{orderId}, клиент {clientId}. Сообщение: {message}");
        }
    }
}