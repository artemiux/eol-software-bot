namespace EolBot.Services.Telegram
{
    public class TelegramSettings
    {
        public required string BotToken { get; set; }

        public required long AdminChatId { get; set; }
    }
}
