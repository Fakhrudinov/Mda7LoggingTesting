using MassTransit;
using Microsoft.Extensions.Logging;
using Restaurant.Messages.Interfaces;
using System.Threading.Tasks;

namespace Restaurant.Booking.Consumers
{
    public class BookingCancelRequested : IConsumer<IBookingCancelRequested>
    {
        private readonly Restaurant _restaurant;
        private readonly ILogger _logger;

        public BookingCancelRequested(Restaurant restaurant, ILogger<BookingCancelRequested> logger)
        {
            _restaurant = restaurant;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<IBookingCancelRequested> context)
        {
            _logger.LogInformation($"BookingCancelRequested==[OrderId {context.Message.OrderId}] Отмена");

            await _restaurant.CancelReservationAsync(context.Message.OrderId, context.CancellationToken);
        }
    }
}
