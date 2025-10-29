using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aviator.Infrastructure
{
    public class CrudServiceAsync<T> : ICrudServiceAsync<T> where T : class
    {
        private readonly IRepository<T> _repository;
        private readonly AviatorContext _context;

        public CrudServiceAsync(IRepository<T> repository, AviatorContext context)
        {
            _repository = repository;
            _context = context;
        }

        public async Task<bool> CreateAsync(T element)
        {
            await _repository.AddAsync(element);
            return await SaveAsync();
        }

        public async Task<T> ReadAsync(int id) => await _repository.GetByIdAsync(Convert.ToInt32(id));

        public async Task<IEnumerable<T>> ReadAllAsync() => await _repository.GetAllAsync();

        public async Task<IEnumerable<T>> ReadAllAsync(int page, int amount)
        {
            var all = await _repository.GetAllAsync();
            return all.Skip((page - 1) * amount).Take(amount);
        }

        public async Task<bool> UpdateAsync(T element)
        {
            await _repository.Update(element);
            return await SaveAsync();
        }

        public async Task<bool> RemoveAsync(T element)
        {
            await _repository.Delete(element);
            return await SaveAsync();
        }

        public async Task<bool> SaveAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
