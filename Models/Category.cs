namespace WebApp.Models;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public List<GoalItem> Goals { get; set; } = new List<GoalItem>();
    public int CompletionStreak { get; set; } = 0;
    public DateTime LastCompletedDate { get; set; }
}