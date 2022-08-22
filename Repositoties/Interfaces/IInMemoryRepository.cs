using System.Collections.Generic;

namespace Restaurant.Messages.Repositories.Interfaces
{
    public interface IInMemoryRepository<T> where T : class
    {
        void AddOrUpdate(T entity);
        IEnumerable<T> Get();
        void Delete();
    }
}
