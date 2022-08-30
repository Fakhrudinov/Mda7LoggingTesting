using System;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Logging;
using Repositories.Interfaces;
using Restaurant.Kitchen.MassTransitDTO;
using Restaurant.Kitchen.Models;
using Restaurant.Messages.CustomExceptions;
using Restaurant.Messages.Interfaces;

namespace Restaurant.Kitchen.Consumers
{
    public class KitchenRequestedConsumer : IConsumer<ITableBooked>
    {
        private readonly Manager _manager;
        private readonly IDataBaseRepositoty<TableBookedModel> _repository;
        private readonly ILogger _logger;

        public KitchenRequestedConsumer(Manager manager, IDataBaseRepositoty<TableBookedModel> repository, ILogger<KitchenFaultConsumer> logger)
        {
            _manager = manager;
            _repository = repository;
            _logger = logger;
        }

        /// <summary>
        /// Подписка на событие бронирования на кухне
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        /// <exception cref="KitchenException"></exception>
        public async Task Consume(ConsumeContext<ITableBooked> context)
        {
            _logger.LogInformation($"KitchenRequestedConsumer request from repository #{context.Message.OrderId}");

            //проверить - уже есть такая запись?
            if (await _repository.Contains(context.MessageId.ToString()))
            {
                _logger.LogDebug("KitchenRequestedConsumer Second time " + context.MessageId.ToString());
                return;
            }

            _logger.LogDebug("KitchenRequestedConsumer First time " + context.MessageId.ToString());

            //добавить в репозиторий
            TableBookedModel requestModel = new TableBookedModel(                
                context.MessageId.ToString(),
                context.Message.OrderId);
            await _repository.Add(requestModel);

            //var randomDelay = new Random().Next(1_000, 10_000);
            var randomDelay = 1;
            _logger.LogDebug($"Kitchen-KitchenRequestedConsumer=Проверим заказ #{context.Message.OrderId} на кухне, это займет {randomDelay}мс");
            await Task.Delay(randomDelay);

            var (confirmation, dish) = _manager.CheckKitchenReady(context.Message.OrderId, context.Message.Dish);
            
            if (confirmation)
            {
                _logger.LogInformation($"Kitchen-KitchenRequestedConsumer=заказ #{context.Message.OrderId} = ok, Publish KitchenReady");
                await context.Publish<IKitchenReady>(new KitchenReady(context.Message.OrderId));
            }
            else
            {
                if (context.Message.Dish.Name != null)
                {
                    _logger.LogWarning($"Kitchen-KitchenRequestedConsumer KitchenException - Заказ с {context.Message.Dish.Name} вызывает у нас проблемы. #{context.Message.OrderId}");
                    throw new KitchenException($"KitchenException - Заказ с {context.Message.Dish.Name} вызывает у нас проблемы. #{context.Message.OrderId}");
                }
                else
                {
                    _logger.LogWarning($"Kitchen-KitchenRequestedConsumer KitchenException - Заказ #{context.Message.OrderId} - у нас нет такого в меню");
                    throw new KitchenException($"KitchenException - Заказ #{context.Message.OrderId} - у нас нет такого в меню");
                }                
            }
        }
    }
}