using Restaurant.Messages;
using Restaurant.Messages.Interfaces;
using System;

namespace Restaurant.Booking.MassTransitDTO
{
    public class TableBooked : ITableBooked
    {
        public Guid OrderId { get; }
        public Guid ClientId { get; }
        public Dish Dish { get; }

        public TableBooked(Guid orderId, Guid clientId, Dish dish = null)
        {
            OrderId = orderId;
            ClientId = clientId;
            Dish = dish;
        }
    }
}