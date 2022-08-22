using System;

namespace Restaurant.Messages.Interfaces
{
    public interface IBookingRequest
    {
        public Guid OrderId { get; }
        public Guid ClientId { get; }
        public Dish? Dish { get; }
        int BookingArrivalTime { get; }
        int ActualArrivalTime { get; }
    }
}
