using Restaurant.Booking.Consumers.Interfaces;
using Restaurant.Booking.Saga;
using System;

namespace Restaurant.Booking.MassTransitDTO
{
    public class GuestWaitingExpired : IGuestWaitingExpired
    {
        private readonly RestaurantBooking _instance;

        public GuestWaitingExpired(RestaurantBooking instance)
        {
            _instance = instance;
        }

        public Guid OrderId => _instance.OrderId;
    }
}
