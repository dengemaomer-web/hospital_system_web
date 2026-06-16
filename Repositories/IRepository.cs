using System.Linq.Expressions;
using HospitalSystem.Models.Entities;

namespace HospitalSystem.Repositories;

public interface IRepository<T> where T : BaseEntity
{
    Task<List<T>> GetAllAsync();
    Task<T?> GetByIdAsync(Guid id);
    Task<List<T>> FindAsync(Func<T, bool> predicate);
    Task<T?> FirstOrDefaultAsync(Func<T, bool> predicate);
    Task<T> AddAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task<bool> DeleteAsync(Guid id);
    Task<int> CountAsync();
    Task<int> CountAsync(Func<T, bool> predicate);
}
