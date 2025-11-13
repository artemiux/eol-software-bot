namespace EolBot.Repositories
{
    public class PaginatedResult<TResult>
    {
        public int? Next { get; init; }

        public required IEnumerable<TResult> Result { get; init; }
    }
}
