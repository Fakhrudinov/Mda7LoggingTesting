using System;

namespace Restaurant.Messages.Interfaces
{
    public interface IBookingCancelRequested
    {
        public Guid OrderId { get; }
    }
}
