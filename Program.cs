using System.Text.Json;
using TelegramSurveyBot.Services;

Console.OutputEncoding = System.Text.Encoding.UTF8;

Console.WriteLine("Telegram Bot ishga tushmoqda...");

// Read configuration
string? botToken = null;

if (File.Exists("appsettings.json"))
{
    var json = await File.ReadAllTextAsync("appsettings.json");
    var config = JsonSerializer.Deserialize<AppSettings>(json);
    botToken = config?.BotConfiguration?.BotToken;
}

// If token not in appsettings, try environment variable
botToken ??= Environment.GetEnvironmentVariable("BotConfiguration:BotToken");

if (string.IsNullOrEmpty(botToken))
{
    Console.WriteLine("XATOLIK: Bot tokenini kiriting!");
    Console.Write("Bot token: ");
    botToken = Console.ReadLine();

    if (string.IsNullOrEmpty(botToken))
    {
        Console.WriteLine("Bot tokensiz ishga tushirib bo'lmaydi!");
        return;
    }
}

var cts = new CancellationTokenSource();

Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

var botService = new BotService(botToken);
await botService.StartAsync(cts.Token);

Console.WriteLine("Botni to'xtatish uchun Ctrl+C bosing...");
await Task.Delay(Timeout.Infinite, cts.Token);

public class AppSettings
{
    public BotConfiguration? BotConfiguration { get; set; }
}

public class BotConfiguration
{
    public string? BotToken { get; set; }
}
