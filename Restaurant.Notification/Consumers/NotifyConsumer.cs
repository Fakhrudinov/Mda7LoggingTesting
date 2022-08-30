using MassTransit;
using Microsoft.Extensions.Logging;
using Restaurant.Messages.Interfaces;
using Restaurant.Messages.Repositories.Interfaces;
using Restaurant.Notification.Models;
using System.Linq;
using System.Threading.Tasks;

namespace Restaurant.Notification.Consumers
{
    public class NotifyConsumer : IConsumer<INotify>
    {
        private readonly Notifier _notifier;
        private readonly IInMemoryRepository<NotifyModel> _repository;
        private readonly ILogger _logger;

        public NotifyConsumer(Notifier notifier, IInMemoryRepository<NotifyModel> repository, ILogger<NotifyConsumer> logger)
        {
            _notifier = notifier;
            _repository = repository;
            _logger = logger;
        }
        /// <summary>
        /// подписка на события уведомлений
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public Task Consume(ConsumeContext<INotify> context)
        {
            _logger.LogInformation($"NotifyConsumer request from repository #{context.Message.OrderId}");

            NotifyModel isNotifierExist = _repository.Get().FirstOrDefault(i => i.OrderId == context.Message.OrderId);

            if (isNotifierExist is not null && isNotifierExist.CheckMessageId(context.MessageId.ToString()))
            {
                _logger.LogDebug("NotifyConsumer Second time " + context.MessageId.ToString());
                return context.ConsumeCompleted;
            }

            _logger.LogDebug("NotifyConsumer First time " + context.MessageId.ToString());

            NotifyModel requestModel = new NotifyModel(
                context.Message.OrderId,
                context.MessageId.ToString());

            NotifyModel resultModel = isNotifierExist?.Update(requestModel, context.Message.ToString()!) ?? requestModel;

            _repository.AddOrUpdate(resultModel);

            _logger.LogDebug($"NotifyConsumer send #{context.Message.OrderId} to client {context.Message.ClientId} message: {context.Message.Message}");
            _notifier.Notify(context.Message.OrderId, context.Message.ClientId, context.Message.Message);
            
            return context.ConsumeCompleted;
        }
    }
}
