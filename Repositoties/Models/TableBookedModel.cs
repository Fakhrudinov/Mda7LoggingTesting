using System;

namespace Restaurant.Kitchen.Models
{
    public class TableBookedModel
    {
        public string MessageId { get; set; }
        public Guid OrderId { get; set; }


        public TableBookedModel(string messageId, Guid orderId)
        {
            MessageId = messageId;

            OrderId = orderId;
        }
    }
}
