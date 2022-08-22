using System;
using System.Collections.Generic;

namespace Restaurant.Booking.Models
{
    public class BookingRequestModel
    {
        private readonly List<string> _messageIds = new();

        public Guid OrderId { get; private set; }

        public BookingRequestModel(Guid orderId, string messageId)
        {
            _messageIds.Add(messageId);

            OrderId = orderId;
        }

        public BookingRequestModel Update(BookingRequestModel model, string messageId)
        {
            _messageIds.Add(messageId);

            OrderId = model.OrderId;

            return this;
        }

        public bool CheckMessageId(string messageId)
        {
            return _messageIds.Contains(messageId);
        }
    }
}
