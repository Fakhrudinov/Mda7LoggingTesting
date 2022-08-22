using MassTransit;
using Restaurant.Messages.Interfaces;
using System;
using System.Threading.Tasks;

namespace Restaurant.Booking.Consumers
{
    public class BookingRequestFaultConsumer : IConsumer<Fault<IBookingRequest>>
    {
        public Task Consume(ConsumeContext<Fault<IBookingRequest>> context)
        {
            Console.WriteLine($"BookingRequestFaultConsumer [OrderId {context.Message.Message.OrderId}] Отмена в зале");
            return Task.CompletedTask;
        }
    }
}
