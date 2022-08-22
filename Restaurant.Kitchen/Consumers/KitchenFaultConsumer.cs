using MassTransit;
using Restaurant.Kitchen.MassTransitDTO;
using Restaurant.Messages.Interfaces;
using System;
using System.Threading.Tasks;

namespace Restaurant.Kitchen.Consumers
{
    public class KitchenFaultConsumer : IConsumer<Fault<ITableBooked>>
    {
        public Task Consume(ConsumeContext<Fault<ITableBooked>> context)
        {
            Console.WriteLine($"KitchenFaultConsumer event for OrderId #{context.Message.Message.OrderId}. Отменяем этот заказ.");

            //context.Publish<IKitchenReject>(new KitchenReject(context.Message.Message.OrderId, context.Message.Message.Dish));

            return Task.CompletedTask;
        }
    }
}
