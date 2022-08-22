using System;

namespace Restaurant.Messages.CustomExceptions
{
    public class KitchenException : Exception
    {
        public KitchenException(string message) : base(message)
        {

        }
    }
}
