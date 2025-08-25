namespace EolBot.Models
{
    public class Report
    {
        public int Id { get; set; }

        public DateTime From { get; set; }

        public DateTime To { get; set; }

        public ICollection<ReportContent> Content { get; set; } = [];

        public DateTime CreatedAt { get; set; }
    }
}
