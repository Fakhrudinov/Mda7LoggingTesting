using Restaurant.Booking.Consumers.Interfaces;
using Restaurant.Booking.Saga;
using System;

namespace Restaurant.Booking.MassTransitDTO
{
    public class GuestArrived : IGuestArrived
    {
        private readonly RestaurantBooking _instance;

        public GuestArrived(RestaurantBooking instance)
        {
            _instance = instance;
        }

        public Guid OrderId => _instance.OrderId;
    }
}
