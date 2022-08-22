using System.Threading.Tasks;

namespace Repositories.Interfaces
{
    public interface IDataBaseRepositoty<T> where T : class
    {
        public Task Add(T entity);
        public Task<bool> Contains(string key);
        //public Task Delete(string key);
    }
}
