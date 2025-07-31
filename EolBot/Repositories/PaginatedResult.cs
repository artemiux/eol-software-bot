namespace EolBot.Repositories
{
    public class PaginatedResult<TResult>
    {
        public int? Next { get; set; }

        public IEnumerable<TResult> Result { get; set; } = [];
    }
}
