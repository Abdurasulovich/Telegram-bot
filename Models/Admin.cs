using System.ComponentModel.DataAnnotations;

namespace TelegramSurveyBot.Models;

public class Admin
{
    [Key]
    public long TelegramId { get; set; }

    public string? Username { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    public long AddedBy { get; set; } // Kim qo'shgan admin
}
