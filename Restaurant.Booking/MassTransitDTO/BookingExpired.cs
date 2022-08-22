using Restaurant.Booking.Consumers.Interfaces;
using Restaurant.Booking.Saga;
using System;

namespace Restaurant.Booking.MassTransitDTO
{
    public class BookingExpired : IBookingExpired
    {
        private readonly RestaurantBooking _instance;

        public BookingExpired(RestaurantBooking instance)
        {
            _instance = instance;
        }

        public Guid OrderId => _instance.OrderId;
    }
}
