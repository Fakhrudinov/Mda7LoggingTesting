using System;

namespace Restaurant.Messages.Interfaces
{
    public interface IBookingReject
    {
        public Guid OrderId { get; }
    }
}
