using System;


namespace Restaurant.Booking.Consumers.Interfaces
{
    public interface IGuestWaitingExpired
    {
        Guid OrderId { get; }
    }
}
