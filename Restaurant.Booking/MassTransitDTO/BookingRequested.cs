using Restaurant.Messages;
using Restaurant.Messages.Interfaces;
using System;


namespace Restaurant.Booking.MassTransitDTO
{
    public class BookingRequested : IBookingRequest
    {
        public BookingRequested(Guid orderId, Guid clientId, Dish dish, int bookingArrivalTime, int actualArrivalTime)
        {
            OrderId = orderId;
            ClientId = clientId;
            Dish = dish;
            BookingArrivalTime = bookingArrivalTime;
            ActualArrivalTime = actualArrivalTime;
        }

        public Guid OrderId { get; }
        public Guid ClientId { get; }
        public Dish Dish { get; }

        public int BookingArrivalTime { get; init; }
        public int ActualArrivalTime { get; init; }
    }
}
