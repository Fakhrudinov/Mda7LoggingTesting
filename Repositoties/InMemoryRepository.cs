using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Timers;
using Restaurant.Messages.Repositories.Interfaces;

namespace Restaurant.Messages.Repositories.Implementation
{
    public class InMemoryRepository<T> : IInMemoryRepository<T> where T : class
    {
        private readonly ConcurrentBag<T> _repository = new();
        private Timer _timer;

        public void AddOrUpdate(T entity)
        {
            _repository.Add(entity);

            _timer = new(30_000);
            _timer.Elapsed += (sender, e) =>  Delete();
            _timer.AutoReset = false;
            _timer.Start();
        }

        public IEnumerable<T> Get()
        {
            return _repository;
        }

        public void Delete()
        {
            _repository.TryTake(out var result);

            System.Console.WriteLine($"InMemoryRepository Delete {result} for {this.GetType()}");
        }
    }
}
