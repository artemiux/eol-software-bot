namespace EolBot.Services.Telegram
{
    public class TelegramSettings
    {
        public required string BotToken { get; init; }

        public required long AdminChatId { get; init; }
    }
}
