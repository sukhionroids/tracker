namespace WebApp.Models;

public class GoalItem
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedDate { get; set; }
    public int Points { get; set; } = 10;
    public string Difficulty { get; set; } = "Normal"; // Easy, Normal, Hard
}