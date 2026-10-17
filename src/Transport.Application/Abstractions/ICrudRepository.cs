namespace Transport.Application.Abstractions;

// CRUD chung cho các danh mục (tuyến, xe, tài xế, hành khách)
public interface ICrudRepository<TEntity>
{
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(TEntity entity, CancellationToken ct = default);
    Task UpdateAsync(TEntity entity, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
