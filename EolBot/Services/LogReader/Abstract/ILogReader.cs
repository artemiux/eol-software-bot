namespace EolBot.Services.LogReader.Abstract
{
    public interface ILogReader
    {
        Task<IEnumerable<string>> TailAsync(string path, int count);
    }
}
