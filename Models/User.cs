namespace WebApp.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public int TotalPoints { get; set; } = 0;
    public int Level { get; set; } = 1;
    public int BalanceBonus { get; set; } = 0;
    public int ConsistencyStreak { get; set; } = 0;
    public DateTime LastActiveDate { get; set; }
    public Dictionary<string, int> CategoryCompletions { get; set; } = new Dictionary<string, int>();
}