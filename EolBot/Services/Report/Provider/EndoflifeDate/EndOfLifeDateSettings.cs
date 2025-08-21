namespace EolBot.Services.Report.Provider.EndoflifeDate
{
    public class EndOfLifeDateSettings
    {
        public required string ApiUrl { get; set; }

        public int? ConnectionTimeout { get; set; }
    }
}
