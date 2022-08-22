using Restaurant.Messages.Interfaces;
using System;

namespace Restaurant.Kitchen.MassTransitDTO
{
    public class KitchenReady : IKitchenReady
    {
        public KitchenReady(Guid orderId)
        {
            OrderId = orderId;
        }

        public Guid OrderId { get; init; }
    }
}
