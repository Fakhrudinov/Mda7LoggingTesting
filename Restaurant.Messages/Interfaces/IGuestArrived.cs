using System;


namespace Restaurant.Booking.Consumers.Interfaces
{
    public interface IGuestArrived
    {
        Guid OrderId { get; }
    }
}
