namespace EolBot.Repositories.Abstract
{
    public interface IQueryableRepository<TEntity>
    {
        IQueryable<TEntity> GetQueryable();
    }
}
