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

        public void AddOrUpdate(T entity)
        {
            _logger.LogInformation($"InMemoryRepository request AddOrUpdate entity={entity}");

            _repository.Add(entity);

            _timer = new(30_000);
            _timer.Elapsed += (sender, e) =>  Delete();
            _timer.AutoReset = false;
            _timer.Start();
        }

        public IEnumerable<T> Get()
        {
            _logger.LogInformation($"InMemoryRepository request Get");

            return _repository;
        }

        public void Delete()
        {
            _repository.TryTake(out var result);

            _logger.LogInformation($"InMemoryRepository request Delete {result}");
        }
    }
}
