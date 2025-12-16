using Microsoft.EntityFrameworkCore;
using TelegramSurveyBot.Models;

namespace TelegramSurveyBot.Data;

public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<UserResponse> UserResponses { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Get the database path - use Data folder if it exists, otherwise use current directory
        var dbPath = GetDatabasePath();
        Console.WriteLine($"[LOG] Database path: {dbPath}");

        optionsBuilder.UseSqlite($"Data Source={dbPath}");
    }

    private static string GetDatabasePath()
    {
        // Try to use Data folder
        var dataFolder = Path.Combine(Directory.GetCurrentDirectory(), "Data");

        // Create Data folder if it doesn't exist
        if (!Directory.Exists(dataFolder))
        {
            try
            {
                Directory.CreateDirectory(dataFolder);
                Console.WriteLine($"[LOG] Data papka yaratildi: {dataFolder}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[XATOLIK] Data papka yaratishda xatolik: {ex.Message}");
                // Fallback to current directory
                return "survey.db";
            }
        }

        File.Delete(dataFolder+"survey.db");
        return Path.Combine(dataFolder, "survey.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasKey(u => u.Id);

        modelBuilder.Entity<UserResponse>()
            .HasOne(r => r.User)
            .WithMany(u => u.Responses)
            .HasForeignKey(r => r.UserId);
    }
}
