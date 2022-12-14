using Automatonymous;
using MassTransit;
using System;


namespace Restaurant.Booking.Saga
{
    public class RestaurantBooking : SagaStateMachineInstance
    {
        //идентификатор для соотнесения всех сообщений друг с другом
        public Guid CorrelationId { get; set; }

        //текущее состояние саги (по умолчанию присутствуют Initial - 1 и Final - 2)
        public int CurrentState { get; set; }

        //идентификатор заказа/бронирования
        public Guid OrderId { get; set; }

        //идентификатор клиента
        public Guid ClientId { get; set; }

        //маркировка для "композиции" событий (наш случай с кухней и забронированным столом)
        public int ReadyEventStatus { get; set; }

        // пометка о том, что наша заявка просрочена
        public Guid? BookingExpirationId { get; set; }
        public Guid? GuestAwaitingId { get; set; }
        public Guid? GuestArrivalId { get; set; }
        public int BookingArrivalTime { get; set; }
        public int ActualArrivalTime { get; set; }
    }
}
