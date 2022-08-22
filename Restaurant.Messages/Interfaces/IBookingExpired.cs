using System;

namespace Restaurant.Booking.Consumers.Interfaces
{
    public interface IBookingExpired
    {
        public Guid OrderId { get; }
    }
}
