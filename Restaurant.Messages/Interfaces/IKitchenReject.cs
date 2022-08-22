using System;

namespace Restaurant.Messages.Interfaces
{
    public interface IKitchenReject
    {
        public Guid OrderId { get; }
    }
}
