using System;

namespace Restaurant.Messages.CustomExceptions
{
    public class BookingException : Exception
    {
        public BookingException(string message) : base(message)
        {

        }
    }
}
