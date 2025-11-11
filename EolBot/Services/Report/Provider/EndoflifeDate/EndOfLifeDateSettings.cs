namespace EolBot.Services.Report.Provider.EndoflifeDate
{
    public class EndOfLifeDateSettings
    {
        public required string ApiUrl { get; init; }

        public int? ConnectionTimeout { get; init; }
    }
}
