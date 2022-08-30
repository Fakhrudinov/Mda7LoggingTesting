using MassTransit;
using Microsoft.Extensions.Logging;
using Restaurant.Messages.Interfaces;
using System.Threading.Tasks;

namespace Restaurant.Booking.Consumers
{
    public class BookingRequestFaultConsumer : IConsumer<Fault<IBookingRequest>>
    {
        private readonly ILogger _logger;

        public BookingRequestFaultConsumer(ILogger<BookingRequestFaultConsumer> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Подписка на событие ошибки про бронировании столика
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public Task Consume(ConsumeContext<Fault<IBookingRequest>> context)
        {
            _logger.LogWarning($"BookingRequestFaultConsumer [OrderId {context.Message.Message.OrderId}] Отмена в зале");
            return Task.CompletedTask;
        }
    }
}
