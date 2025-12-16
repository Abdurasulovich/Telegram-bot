namespace TelegramSurveyBot.Models;

public class User
{
    public long Id { get; set; } // Telegram User ID
    public string? PhoneNumber { get; set; }
    public string? Username { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string SelectedLanguage { get; set; } = ""; // "uz", "kk", "ru"
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    public List<UserResponse> Responses { get; set; } = new();
}
