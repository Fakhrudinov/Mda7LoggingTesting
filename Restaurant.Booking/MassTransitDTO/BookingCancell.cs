using Restaurant.Messages.Interfaces;
using System;

namespace Restaurant.Booking.MassTransitDTO
{
    public class BookingCancell : IBookingCancelRequested
    {
        public BookingCancell(Guid orderId)
        {
            OrderId = orderId;
        }

        public Guid OrderId { get; }
    }
}
