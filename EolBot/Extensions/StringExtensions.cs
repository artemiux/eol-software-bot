using System.Diagnostics.CodeAnalysis;

namespace EolBot.Extensions
{
    static class StringExtensions
    {
        extension(string source)
        {
            internal string Truncate(int length, [AllowNull] string suffix = "")
            {
                if (length <= 0)
                {
                    throw new ArgumentException("Must be positive", nameof(length));
                }
                if (source.Length <= length)
                {
                    return source;
                }
                length -= suffix?.Length ?? 0;
                return length <= 0 ? source : string.Concat(source.AsSpan(0, length), suffix);
            }
        }
    }
}
