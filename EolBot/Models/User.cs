namespace EolBot.Models
{
    public class User
    {
        public long TelegramId { get; set; }

        public bool IsActive { get; set; }

        public string? LanguageCode { get; set; }

        public DateTime? SubscribedAt { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
