using EolBot.Services.LogReader.Abstract;

namespace EolBot.Services.LogReader
{
    public class FileLogReader : ILogReader
    {
        public async Task<IEnumerable<string>> TailAsync(string path, int count)
        {
            string filePath;
            if (File.Exists(path))
            {
                filePath = path;
            }
            else if (Directory.Exists(path))
            {
                filePath = Directory.EnumerateFiles(path)
                    .OrderByDescending(File.GetCreationTime)
                    .FirstOrDefault() ?? throw new ArgumentException($"Directory does not contain any files: {path}.");
            }
            else
            {
                throw new InvalidOperationException($"Not found: {path}.");
            }

            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fs);

            LinkedList<string> result = new();
            while (await reader.ReadLineAsync() is { } line)
            {
                result.AddLast(line);
                // Keep in memory no more than `count` lines.
                if (result.Count > count)
                {
                    result.RemoveFirst();
                }
            }

            return result;
        }
    }
}
