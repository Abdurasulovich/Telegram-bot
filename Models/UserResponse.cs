namespace TelegramSurveyBot.Models;

public class UserResponse
{
    public int Id { get; set; }
    public long UserId { get; set; }
    public User User { get; set; } = null!;

    public string SurveyType { get; set; } = ""; // "corruption" or "teacher_evaluation"
    public int QuestionNumber { get; set; }
    public string QuestionText { get; set; } = "";
    public string AnswerText { get; set; } = "";
    public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;
}
