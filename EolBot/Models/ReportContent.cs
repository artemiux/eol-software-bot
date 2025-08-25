using System.Diagnostics.CodeAnalysis;

namespace EolBot.Models
{
    public class ReportContent
    {
        public int Id { get; set; }

        public required string ProductName { get; set; }

        public required string ProductVersion { get; set; }

        public string? ProductUrl { get; set; }

        public required DateTime Eol { get; set; }

        public DateTime CreatedAt { get; set; }

        [SetsRequiredMembers]
        public ReportContent(
            string name, string version, DateTime eol, string? url = null)
        {
            ProductName = name;
            ProductVersion = version;
            ProductUrl = url;
            Eol = eol;
        }

        private ReportContent() { }
    }
}
