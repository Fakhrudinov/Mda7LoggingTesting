using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Timers;
using Microsoft.Extensions.Logging;
using Restaurant.Messages.Repositories.Interfaces;

namespace Restaurant.Messages.Repositories.Implementation
{
    public class InMemoryRepository<T> : IInMemoryRepository<T> where T : class
    {
        private readonly ConcurrentBag<T> _repository = new();
        private Timer _timer;
        private readonly ILogger _logger;

        public InMemoryRepository(ILogger<T> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Добавление новой уникальной записи. Запуск таймера на удаление записи через 30 секунд
        /// </summary>
        /// <param name="entity">T class</param>
        public void AddOrUpdate(T entity)
        {
            _logger.LogInformation($"InMemoryRepository request AddOrUpdate entity={entity}");

            _repository.Add(entity);

            _timer = new(30_000);
            _timer.Elapsed += (sender, e) =>  Delete();
            _timer.AutoReset = false;
            _timer.Start();
        }

        /// <summary>
        /// Получение всех записей
        /// </summary>
        /// <returns></returns>
        public IEnumerable<T> Get()
        {
            _logger.LogInformation($"InMemoryRepository request Get");

            return _repository;
        }

        /// <summary>
        /// Удаление одной записи
        /// </summary>
        public void Delete()
        {
            _repository.TryTake(out var result);

            _logger.LogInformation($"InMemoryRepository request Delete {result}");
        }
    }
}
