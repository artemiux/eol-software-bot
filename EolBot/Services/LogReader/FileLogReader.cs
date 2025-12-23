using EolBot.Services.LogReader.Abstract;

namespace EolBot.Services.LogReader
{
    public class FileLogReader : ILogReader
    {
        public async Task<IEnumerable<string>> TailAsync(string path, int count)
        {
            path = path switch
            {
                _ when File.Exists(path) => path,
                _ when Directory.Exists(path) => Directory.EnumerateFiles(path)
                    .OrderByDescending(File.GetCreationTime)
                    .FirstOrDefault() ?? throw new FileNotFoundException($"Directory '{path}' does not contain any files."),
                _ => throw new FileNotFoundException()
            };

            await using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            var lines = new LinkedList<string>();
            var line = new LinkedList<char>();

            const int bufferSize = 4096;
            var buffer = new byte[bufferSize];
            long position = fs.Length;
            while (position > 0 && lines.Count < count)
            {
                var toRead = (int)Math.Min(bufferSize, position);
                position -= toRead;
                fs.Position = position;
                await fs.ReadExactlyAsync(buffer, 0, toRead);

                for (int i = toRead - 1; i >= 0; i--)
                {
                    var c = (char)buffer[i];
                    if (c != '\n')
                    {
                        line.AddFirst(c);
                    }
                    // Ignore empty lines.
                    else if (line.Count > 0)
                    {
                        lines.AddFirst(line.AsString());
                        line.Clear();
                        // All lines were found while reading the buffer.
                        if (lines.Count == count)
                        {
                            break;
                        }
                    }
                }
            }

            // Add remaining characters.
            if (line.Count > 0 && lines.Count < count)
            {
                lines.AddFirst(line.AsString());
            }

            return lines;
        }
    }

    file static class LinkedListExtensions
    {
        extension(LinkedList<char> list)
        {
            internal string AsString() =>
                string.Create(list.Count, list, AsStringCallback);
        }

        private static void AsStringCallback(Span<char> span, LinkedList<char> list)
        {
            int index = 0;
            foreach (char c in list)
            {
                span[index++] = c;
            }
        }
    }
}
