using Restaurant.Messages;
using Restaurant.Messages.Interfaces;
using System;


namespace Restaurant.Kitchen.MassTransitDTO
{
    public class KitchenReject : IKitchenReject
    {
        public KitchenReject(Guid orderId, Dish dish)
        {
            OrderId = orderId;
            this.dish = dish;
        }

        public Dish dish;
        public Guid OrderId { get; init; }
    }
}
