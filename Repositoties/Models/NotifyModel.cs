using System;
using System.Collections.Generic;

namespace Restaurant.Notification.Models
{
    public class NotifyModel
    {
        private readonly List<string> _messageIds = new();

        public Guid OrderId { get; private set; }

        public NotifyModel(Guid orderId, string messageId)
        {
            _messageIds.Add(messageId);

            OrderId = orderId;
        }

        public NotifyModel Update(NotifyModel model, string messageId)
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
