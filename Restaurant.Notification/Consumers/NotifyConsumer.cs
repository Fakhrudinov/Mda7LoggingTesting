using MassTransit;
using Restaurant.Messages.Interfaces;
using Restaurant.Messages.Repositories.Interfaces;
using Restaurant.Notification.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Restaurant.Notification.Consumers
{
    public class NotifyConsumer : IConsumer<INotify>
    {
        private readonly Notifier _notifier;
        private readonly IInMemoryRepository<NotifyModel> _repository;

        public NotifyConsumer(Notifier notifier, IInMemoryRepository<NotifyModel> repository)
        {
            _notifier = notifier;
            _repository = repository;
        }

        public Task Consume(ConsumeContext<INotify> context)
        {
            NotifyModel isNotifierExist = _repository.Get().FirstOrDefault(i => i.OrderId == context.Message.OrderId);

            if (isNotifierExist is not null && isNotifierExist.CheckMessageId(context.MessageId.ToString()))
            {
                Console.WriteLine("Second time " + context.MessageId.ToString());
                return context.ConsumeCompleted;
            }

            Console.WriteLine("First time " + context.MessageId.ToString());

            NotifyModel requestModel = new NotifyModel(
                context.Message.OrderId,
                context.MessageId.ToString());

            NotifyModel resultModel = isNotifierExist?.Update(requestModel, context.Message.ToString()!) ?? requestModel;

            _repository.AddOrUpdate(resultModel);

            _notifier.Notify(context.Message.OrderId, context.Message.ClientId, context.Message.Message);

            return context.ConsumeCompleted;
        }
    }
}
