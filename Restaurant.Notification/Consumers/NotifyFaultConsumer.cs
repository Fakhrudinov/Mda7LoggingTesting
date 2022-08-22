using MassTransit;
using Restaurant.Messages.Interfaces;
using System;
using System.Threading.Tasks;

namespace Restaurant.Notification.Consumers
{
    public class NotifyFaultConsumer : IConsumer<Fault<INotify>>
    {
        public Task Consume(ConsumeContext<Fault<INotify>> context)
        {
            Console.WriteLine($"NotifyFaultConsumer Event for {context.Message.Message.OrderId}");
            return Task.CompletedTask;
        }
    }
}
