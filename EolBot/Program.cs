using EolBot.Database;
using EolBot.Repositories;
using EolBot.Repositories.Abstract;
using EolBot.Services;
using EolBot.Services.Localization;
using EolBot.Services.Localization.Abstract;
using EolBot.Services.LogReader;
using EolBot.Services.LogReader.Abstract;
using EolBot.Services.Report;
using EolBot.Services.Report.Abstract;
using EolBot.Services.Report.EndoflifeDate;
using EolBot.Services.Report.Provider.Abstract;
using EolBot.Services.Report.Provider.EndoflifeDate;
using EolBot.Services.Report.Provider.EndoflifeDate.Api;
using EolBot.Services.Report.Provider.EndoflifeDate.Api.Abstract;
using EolBot.Services.Telegram;
using EolBot.Services.Telegram.Bot;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;
using Telegram.Bot;

namespace EolBot
{
    class Program
    {
        async static Task Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            // Setup settings.
            builder.Services.Configure<TelegramSettings>(builder.Configuration.GetSection("Telegram"));
            builder.Services.Configure<ReportSettings>(builder.Configuration.GetSection("Report"));
            builder.Services.Configure<EndOfLifeDateSettings>(builder.Configuration.GetSection("EndOfLifeDate"));
            builder.Services.Configure<LogReaderSettings>(builder.Configuration.GetSection("LogReader"));

            // Setup repositories.
            builder.Services.AddDbContext<EolBotDbContext>();
            builder.Services.AddScoped<IUserRepository, DatabaseUserRepository>();
            builder.Services.AddScoped<IReportRepository, DatabaseReportRepository>();

            // Setup report services.
            builder.Services.AddHttpClient("endoflifedate_client").RemoveAllLoggers()
                .AddTypedClient<IEndOfLifeDateClient>((httpClient, sp) =>
                {
                    EndOfLifeDateSettings? settings = sp.GetService<IOptions<EndOfLifeDateSettings>>()?.Value;
                    ArgumentNullException.ThrowIfNull(settings);
                    httpClient.BaseAddress = new Uri(settings.ApiUrl);
                    if (settings.ConnectionTimeout.HasValue)
                    {
                        httpClient.Timeout = TimeSpan.FromSeconds(settings.ConnectionTimeout.Value);
                    }
                    return new EndOfLifeDateClient(httpClient);
                });
            builder.Services.AddSingleton<IReportDataProvider, EndofLifeDateProvider>();
            builder.Services.AddSingleton<IReport, EndoflifeDateDailyReport>();

            // Setup Telegram services.
            builder.Services.AddSingleton<TelegramSender>();
            builder.Services.AddHttpClient("telegram_bot_client").RemoveAllLoggers()
                .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
                {
                    TelegramSettings? settings = sp.GetService<IOptions<TelegramSettings>>()?.Value;
                    ArgumentNullException.ThrowIfNull(settings);
                    TelegramBotClientOptions options = new(settings.BotToken);
                    return new TelegramBotClient(options, httpClient);
                });
            builder.Services.AddScoped<UpdateHandler>();
            builder.Services.AddScoped<ReceiverService>();
            builder.Services.AddHostedService<PollingService>();

            // Setup jobs.
            builder.Services.AddHangfire(conf =>
            {
                conf.UseMemoryStorage();
            });
            builder.Services.AddHangfireServer();
            builder.Services.AddTransient<Jobs>();

            // Setup logging.
            builder.Services.AddSingleton<ILogReader, FileLogReader>();
            builder.Services.AddSerilog((services, loggerConfiguration) => loggerConfiguration
                .ReadFrom.Configuration(builder.Configuration));

            // Setup localization.
            builder.Services.AddSingleton<ILocalizationService, LocalizationService>();

            var host = builder.Build();

            Directory.CreateDirectory("AppData");

            if (builder.Environment.IsDevelopment())
            {
                var options = host.Services.GetRequiredService<IOptions<TelegramSettings>>();
                using var scope = host.Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<EolBotDbContext>();
                context.Database.Migrate();
                await DataSeeder.SeedAsync(context, options);
            }
            else
            {
                var jobManager = host.Services.GetRequiredService<IRecurringJobManager>();
                jobManager.AddOrUpdate<Jobs>(
                    "weeklyreportjob",
                    (jobs) => jobs.SendWeeklyReportAsync(default!),
                    Cron.Weekly(DayOfWeek.Monday, 0, 10));
            }

            host.Run();
        }
    }
}
