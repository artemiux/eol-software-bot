namespace EolBot.Models
{
    public class Report
    {
        public int Id { get; set; }

        public required string Data { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
