using MassTransit;
using Restaurant.Messages.Interfaces;
using System;
using System.Threading.Tasks;

namespace Restaurant.Booking.Consumers
{
    public class BookingCancelRequested : IConsumer<IBookingCancelRequested>
    {
        private readonly Restaurant _restaurant;

        public BookingCancelRequested(Restaurant restaurant)
        {
            _restaurant = restaurant;
        }

        public async Task Consume(ConsumeContext<IBookingCancelRequested> context)
        {
            Console.WriteLine($"BookingCancelRequested==[OrderId {context.Message.OrderId}] Отмена");

            await _restaurant.CancelReservationAsync(context.Message.OrderId, context.CancellationToken);
        }
    }
}
