using System;

namespace Restaurant.Messages.Interfaces
{
    public interface ITableBooked
    {
        public Guid OrderId { get; }
        public Guid ClientId { get; }
        Dish? Dish { get; }
    }
}
