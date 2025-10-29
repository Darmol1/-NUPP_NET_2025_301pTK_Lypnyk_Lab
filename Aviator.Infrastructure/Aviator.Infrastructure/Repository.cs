using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aviator.Infrastructure
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly AviatorContext _context;
        private readonly DbSet<T> _dbSet;

        public Repository(AviatorContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public async Task<T> GetByIdAsync(int id) => await _dbSet.FindAsync(id);

        public async Task<IEnumerable<T>> GetAllAsync() => await _dbSet.ToListAsync();

        public async Task AddAsync(T entity) => await _dbSet.AddAsync(entity);

        public async Task Update(T entity) => _dbSet.Update(entity);

        public async Task Delete(T entity) => _dbSet.Remove(entity);
    }
}
