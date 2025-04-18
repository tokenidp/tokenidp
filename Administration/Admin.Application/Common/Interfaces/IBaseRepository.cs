using System.Linq.Expressions;

namespace Identity.Application.Common.Interfaces;

public interface IBaseRepository<TEntity>
    where TEntity : IAggregateRoot
{
    /// <summary>
    /// Adds new object async
    /// </summary>
    /// <param name="entity">object that need to be added</param>  
    /// <returns>Task.</returns>
    Task<TEntity> AddAsync(TEntity entity);

    /// <summary>
    /// Update entity partially
    /// </summary>
    /// <typeparam name="TEntity">entity type</typeparam>
    /// <param name="entity">object that need to be update partially</param>
    void Attach(TEntity entity);

    /// <summary>
    /// Find matching objects
    /// </summary>
    /// <param name="predicate">expression</param>
    /// <returns></returns>
    Task<TEntity> FindAsync(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    /// Find matching objects
    /// </summary>
    /// <param name="predicate">expression</param>
    /// <returns></returns>
    Task<List<TEntity>> FindAllAsync(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    /// Gets all objects async
    /// </summary>
    /// <returns>List of objects</returns>
    Task<List<TEntity>> GetAllAsync();

    /// <summary>
    /// Get object by id async
    /// </summary>
    /// <param name="id">primary key</param>
    /// <returns></returns>
    Task<TEntity> GetByIdAsync(int id);

    /// <summary>
    /// Remove object async
    /// </summary>
    /// <param name="entity">object that need to be removed</param>
    Task DeleteAsync(TEntity entity);

    /// <summary>
    /// Remove more than one object
    /// </summary>
    /// <param name="entities">objects that need to be removed</param>
    Task DeleteRangeAsync(IEnumerable<TEntity> entities);

    /// <summary>
    /// update existing object
    /// </summary>
    /// <param name="entity">object that need to be updated</param>
    Task<TEntity> UpdateAsync(TEntity entity);
}
