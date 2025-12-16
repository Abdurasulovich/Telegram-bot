using Microsoft.EntityFrameworkCore;
using TelegramSurveyBot.Models;

namespace TelegramSurveyBot.Data;

public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<UserResponse> UserResponses { get; set; }
    public DbSet<Admin> Admins { get; set; }

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

        var dbPath = Path.Combine(dataFolder, "survey.db");

        // Delete old database to recreate with new schema (comment out after first run)
        if (File.Exists(dbPath))
        {
            try
            {
                File.Delete(dbPath);
                Console.WriteLine($"[LOG] Eski database o'chirildi: {dbPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[XATOLIK] Database o'chirishda xatolik: {ex.Message}");
            }
        }

        return dbPath;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasKey(u => u.Id);

        modelBuilder.Entity<UserResponse>()
            .HasOne(r => r.User)
            .WithMany(u => u.Responses)
            .HasForeignKey(r => r.UserId);

        modelBuilder.Entity<Admin>()
            .HasKey(a => a.TelegramId);

        // Seed initial admin
        modelBuilder.Entity<Admin>().HasData(
            new Admin
            {
                TelegramId = 8022427685,
                FirstName = "Initial Admin",
                AddedAt = DateTime.UtcNow,
                AddedBy = 8022427685 // Self-added
            }
        );
    }
}
