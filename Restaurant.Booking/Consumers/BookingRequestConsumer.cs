using MassTransit;
using Restaurant.Messages.Interfaces;
using System.Threading.Tasks;
using Restaurant.Booking.MassTransitDTO;
using Restaurant.Messages.CustomExceptions;
using Restaurant.Booking.Models;
using System.Linq;
using Restaurant.Messages.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace Restaurant.Booking.Consumers
{
    public class BookingRequestConsumer : IConsumer<IBookingRequest>
    {
        private readonly Restaurant _restaurant;
        private readonly IInMemoryRepository<BookingRequestModel> _repository;
        private readonly ILogger _logger;

        public BookingRequestConsumer(Restaurant restaurant, IInMemoryRepository<BookingRequestModel> repository, ILogger<BookingRequestConsumer> logger)
        {
            _restaurant = restaurant;
            _repository = repository;
            _logger = logger;
        }

        /// <summary>
        /// Подписка на событие запрос бронирования столика
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        /// <exception cref="BookingException"></exception>
        public async Task Consume(ConsumeContext<IBookingRequest> context)
        {
            _logger.LogInformation($"BookingRequestConsumer==[OrderId: {context.Message.OrderId}] Ищем свободный стол");

            var isBookingRequestExist = _repository.Get().FirstOrDefault(model => model.OrderId == context.Message.OrderId);

            if (isBookingRequestExist is not null && isBookingRequestExist.CheckMessageId(context.MessageId.ToString()))
            {
                _logger.LogDebug("BookingRequestConsumer Second time " + context.MessageId.ToString());
                return;
            }

            var requestModel = new BookingRequestModel(
                context.Message.OrderId,
                context.MessageId.ToString());

            _logger.LogDebug("BookingRequestConsumer First time " + context.MessageId.ToString());
            var resultModel = isBookingRequestExist?.Update(requestModel, context.MessageId.ToString()) ?? requestModel;

            _repository.AddOrUpdate(resultModel);

            // есть свободный стол?
            var result = await _restaurant.BookFreeTableAsync(1, context.Message.OrderId, context.CancellationToken);

            if (result == true)
            {
                _logger.LogInformation($"BookingRequestConsumer==[OrderId: {context.Message.OrderId}] Столик найден");
                await context.Publish<ITableBooked>(
                    new TableBooked(
                        context.Message.OrderId,
                        context.Message.ClientId, 
                        context.Message.Dish));
            }
            else
            {
                _logger.LogWarning($"BookingRequestConsumer== [OrderId: {context.Message.OrderId}] Ошибка бронирования!");

                //await context.Publish<IBookingReject>(new BookingReject(context.Message.OrderId));
                throw new BookingException($"Ошибка бронирования! для заказа {context.Message.OrderId}");
            }
        }
    }
}
