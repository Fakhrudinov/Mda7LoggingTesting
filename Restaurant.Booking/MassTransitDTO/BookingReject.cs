using System;
using Restaurant.Messages.Interfaces;


namespace Restaurant.Booking.MassTransitDTO
{
    public class BookingReject : IBookingReject
    {
        public Guid OrderId { get; }

        public BookingReject(Guid orderId)
        {
            OrderId = orderId;
        }
    }
}
