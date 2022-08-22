using System;
using Restaurant.Messages;

namespace Restaurant.Kitchen
{
    public class Manager
    {
        public (bool confirmation, Dish? dish) CheckKitchenReady(Guid orderId, Dish? dish)
        {
            switch (dish.Id)
            {
                case (int)EnumDishes.Chicken:
                case (int)EnumDishes.Pizza:
                case (int)EnumDishes.Pasta:
                    return (true, dish);
                case (int)EnumDishes.Lasagna:
                    dish.Name = EnumDishes.Lasagna.ToString();
                    return (false, dish);
                default:
                    return (false, dish);
            }
        }
    }
}