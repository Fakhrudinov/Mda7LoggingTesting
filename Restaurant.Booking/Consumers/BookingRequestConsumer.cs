using MassTransit;
using Restaurant.Messages.Interfaces;
using System;
using System.Threading.Tasks;
using Restaurant.Booking.MassTransitDTO;
using Restaurant.Messages.CustomExceptions;
using Restaurant.Booking.Models;
using System.Linq;
using Restaurant.Messages.Repositories.Interfaces;

namespace Restaurant.Booking.Consumers
{
    public class BookingRequestConsumer : IConsumer<IBookingRequest>
    {
        private readonly Restaurant _restaurant;
        private readonly IInMemoryRepository<BookingRequestModel> _repository;

        public BookingRequestConsumer(Restaurant restaurant, IInMemoryRepository<BookingRequestModel> repository)
        {
            _restaurant = restaurant;
            _repository = repository;
        }

        public async Task Consume(ConsumeContext<IBookingRequest> context)
        {
            Console.WriteLine($"BookingRequestConsumer==[OrderId: {context.Message.OrderId}] Ищем свободный стол");

            var isBookingRequestExist = _repository.Get().FirstOrDefault(model => model.OrderId == context.Message.OrderId);

            if (isBookingRequestExist is not null && isBookingRequestExist.CheckMessageId(context.MessageId.ToString()))
            {
                Console.WriteLine("Second time " + context.MessageId.ToString());
                return;
            }

            var requestModel = new BookingRequestModel(
                context.Message.OrderId,
                context.MessageId.ToString());

            Console.WriteLine("First time " + context.MessageId.ToString());
            var resultModel = isBookingRequestExist?.Update(requestModel, context.MessageId.ToString()) ?? requestModel;

            _repository.AddOrUpdate(resultModel);

            // есть свободный стол?
            var result = await _restaurant.BookFreeTableAsync(1, context.Message.OrderId, context.CancellationToken);

            if (result == true)
            {
                Console.WriteLine($"BookingRequestConsumer==[OrderId: {context.Message.OrderId}] Столик найден");
                await context.Publish<ITableBooked>(
                    new TableBooked(
                        context.Message.OrderId,
                        context.Message.ClientId, 
                        context.Message.Dish));
            }
            else
            {
                Console.WriteLine($"BookingRequestConsumer== [OrderId: {context.Message.OrderId}] Ошибка бронирования!");

                //await context.Publish<IBookingReject>(new BookingReject(context.Message.OrderId));
                throw new BookingException($"Ошибка бронирования! для заказа {context.Message.OrderId}");
            }
        }
    }
}
