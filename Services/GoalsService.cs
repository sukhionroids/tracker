using System.Text.Json;
using WebApp.Models;

namespace WebApp.Services;

public class GoalsService
{
    private List<Category> _categories = new();
    private User _currentUser = new();
    private readonly ILogger<GoalsService> _logger;
    private readonly AzureStorageService _storageService;

    // Constants for gamification
    private const int BALANCE_THRESHOLD = 1; // Minimum goals completed in each category to get balance bonus
    private const int BALANCE_BONUS_POINTS = 50;
    private const int STREAK_BONUS = 10; // Points per day of streak

    // Azure storage file names
    private const string CATEGORIES_FILENAME = "categories.json";
    private const string USER_FILENAME = "user.json";

    public GoalsService(ILogger<GoalsService> logger, AzureStorageService storageService)
    {
        _logger = logger;
        _storageService = storageService;
        InitializeDataAsync().ConfigureAwait(false);
    }

    public List<Category> GetCategories() => _categories;

    public Category? GetCategory(int id) => _categories.FirstOrDefault(c => c.Id == id);

    public User GetCurrentUser() => _currentUser;

    // Mark a goal as completed
    public async Task CompleteGoalAsync(int categoryId, int goalId)
    {
        var category = _categories.FirstOrDefault(c => c.Id == categoryId);
        if (category == null) return;

        var goal = category.Goals.FirstOrDefault(g => g.Id == goalId);
        if (goal == null) return;

        // If goal is already completed today, do nothing
        if (goal.IsCompleted && goal.CompletedDate?.Date == DateTime.Now.Date)
            return;

        goal.IsCompleted = true;
        goal.CompletedDate = DateTime.Now;

        // Update user points
        _currentUser.TotalPoints += goal.Points;
        
        // Update category streak
        if (category.LastCompletedDate.Date != DateTime.Now.Date)
        {
            if (category.LastCompletedDate.Date == DateTime.Now.AddDays(-1).Date)
            {
                // Continue streak
                category.CompletionStreak++;
            }
            else
            {
                // Broke streak, restart
                category.CompletionStreak = 1;
            }
            category.LastCompletedDate = DateTime.Now;
        }

        // Update user category completions
        if (!_currentUser.CategoryCompletions.ContainsKey(category.Name))
        {
            _currentUser.CategoryCompletions[category.Name] = 0;
        }
        _currentUser.CategoryCompletions[category.Name]++;

        // Check for balance bonus
        CheckForBalanceBonus();

        // Update user streak
        UpdateUserStreak();

        // Update user level
        UpdateUserLevel();

        await SaveDataAsync();
    }

    // For backward compatibility with synchronous code
    public void CompleteGoal(int categoryId, int goalId)
    {
        CompleteGoalAsync(categoryId, goalId).Wait();
    }

    // Reset a goal to not completed
    public async Task ResetGoalAsync(int categoryId, int goalId)
    {
        var category = _categories.FirstOrDefault(c => c.Id == categoryId);
        if (category == null) return;

        var goal = category.Goals.FirstOrDefault(g => g.Id == goalId);
        if (goal == null) return;

        if (goal.IsCompleted)
        {
            goal.IsCompleted = false;
            _currentUser.TotalPoints -= goal.Points;
            await SaveDataAsync();
        }
    }

    // For backward compatibility with synchronous code
    public void ResetGoal(int categoryId, int goalId)
    {
        ResetGoalAsync(categoryId, goalId).Wait();
    }

    // Add a new goal to a category
    public async Task AddGoalAsync(int categoryId, string description, string difficulty = "Normal")
    {
        var category = _categories.FirstOrDefault(c => c.Id == categoryId);
        if (category == null) return;

        int points = difficulty switch
        {
            "Easy" => 5,
            "Normal" => 10,
            "Hard" => 20,
            _ => 10
        };

        var newGoal = new GoalItem
        {
            Id = category.Goals.Count > 0 ? category.Goals.Max(g => g.Id) + 1 : 1,
            Description = description,
            CategoryId = categoryId,
            Points = points,
            Difficulty = difficulty
        };

        category.Goals.Add(newGoal);
        await SaveDataAsync();
    }

    // For backward compatibility with synchronous code
    public void AddGoal(int categoryId, string description, string difficulty = "Normal")
    {
        AddGoalAsync(categoryId, description, difficulty).Wait();
    }

    // Check if user completed at least one goal in each category today
    private void CheckForBalanceBonus()
    {
        // Check if all categories have at least BALANCE_THRESHOLD goals completed today
        bool allCategoriesActive = _categories.All(c => 
            c.Goals.Count(g => 
                g.IsCompleted && 
                g.CompletedDate?.Date == DateTime.Now.Date) >= BALANCE_THRESHOLD);

        if (allCategoriesActive)
        {
            _currentUser.TotalPoints += BALANCE_BONUS_POINTS;
            _currentUser.BalanceBonus++;
            _logger.LogInformation($"Balance bonus awarded: +{BALANCE_BONUS_POINTS} points!");
        }
    }

    // Update user streak if they've been active daily
    private void UpdateUserStreak()
    {
        if (_currentUser.LastActiveDate.Date == DateTime.Now.AddDays(-1).Date)
        {
            _currentUser.ConsistencyStreak++;
            _currentUser.TotalPoints += STREAK_BONUS * _currentUser.ConsistencyStreak;
            _logger.LogInformation($"Streak bonus: +{STREAK_BONUS * _currentUser.ConsistencyStreak} points!");
        }
        else if (_currentUser.LastActiveDate.Date != DateTime.Now.Date)
        {
            _currentUser.ConsistencyStreak = 1;
        }

        _currentUser.LastActiveDate = DateTime.Now;
    }

    // Update user level based on total points
    private void UpdateUserLevel()
    {
        int calculatedLevel = 1 + (_currentUser.TotalPoints / 100);
        if (calculatedLevel > _currentUser.Level)
        {
            _currentUser.Level = calculatedLevel;
            _logger.LogInformation($"Level up! You are now level {_currentUser.Level}");
        }
    }

    // Initialize with categories from goals.txt if available
    private async Task InitializeDataAsync()
    {
        try
        {
            await LoadFromAzureStorageAsync();

            // If no data loaded, initialize with default data
            if (_categories.Count == 0)
            {
                await InitializeFromGoalsFileAsync();
            }

            // Initialize user if not loaded
            if (_currentUser.Username == string.Empty)
            {
                _currentUser = new User
                {
                    Id = 1,
                    Username = "User",
                    LastActiveDate = DateTime.Now
                };
                await SaveDataAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error initializing data: {ex.Message}");
            await InitializeWithDefaultDataAsync();
        }
    }

    private async Task InitializeFromGoalsFileAsync()
    {
        try
        {
            string goalsFilePath = "goals.txt";
            if (File.Exists(goalsFilePath))
            {
                string[] lines = await File.ReadAllLinesAsync(goalsFilePath);
                int categoryId = 1;
                Category? currentCategory = null;
                int goalId = 1;

                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    if (line.EndsWith(':'))
                    {
                        // This is a category line
                        string categoryName = line.TrimEnd(':');
                        currentCategory = new Category
                        {
                            Id = categoryId++,
                            Name = categoryName,
                            Icon = GetIconForCategory(categoryName),
                            Color = GetColorForCategory(categoryName),
                            LastCompletedDate = DateTime.Now.AddDays(-2) // Set to before yesterday
                        };
                        _categories.Add(currentCategory);
                    }
                    else if (line.StartsWith('-') && currentCategory != null)
                    {
                        // This is a goal line
                        string goalDescription = line.Substring(1).Trim();
                        currentCategory.Goals.Add(new GoalItem
                        {
                            Id = goalId++,
                            Description = goalDescription,
                            CategoryId = currentCategory.Id,
                            Points = 10, // Default points
                            Difficulty = "Normal" // Default difficulty
                        });
                    }
                }

                await SaveDataAsync();
            }
            else
            {
                await InitializeWithDefaultDataAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error reading goals.txt: {ex.Message}");
            await InitializeWithDefaultDataAsync();
        }
    }

    // Initialize with default data if loading fails
    private async Task InitializeWithDefaultDataAsync()
    {
        _categories = new List<Category>
        {
            new Category
            {
                Id = 1,
                Name = "Career",
                Icon = "bi-briefcase",
                Color = "#4285F4",
                LastCompletedDate = DateTime.Now.AddDays(-2),
                Goals = new List<GoalItem>
                {
                    new GoalItem { Id = 1, Description = "Apply for one job", CategoryId = 1, Points = 10, Difficulty = "Normal" },
                    new GoalItem { Id = 2, Description = "Update LinkedIn profile", CategoryId = 1, Points = 5, Difficulty = "Easy" },
                    new GoalItem { Id = 3, Description = "Learn one new professional skill", CategoryId = 1, Points = 20, Difficulty = "Hard" }
                }
            },
            new Category
            {
                Id = 2,
                Name = "Education",
                Icon = "bi-book",
                Color = "#34A853",
                LastCompletedDate = DateTime.Now.AddDays(-2),
                Goals = new List<GoalItem>
                {
                    new GoalItem { Id = 4, Description = "Read 20 pages of a book", CategoryId = 2, Points = 10, Difficulty = "Normal" },
                    new GoalItem { Id = 5, Description = "Watch one educational video", CategoryId = 2, Points = 5, Difficulty = "Easy" },
                    new GoalItem { Id = 6, Description = "Practice a language for 15 minutes", CategoryId = 2, Points = 10, Difficulty = "Normal" }
                }
            },
            new Category
            {
                Id = 3,
                Name = "Health",
                Icon = "bi-heart",
                Color = "#EA4335",
                LastCompletedDate = DateTime.Now.AddDays(-2),
                Goals = new List<GoalItem>
                {
                    new GoalItem { Id = 7, Description = "Exercise for 30 minutes", CategoryId = 3, Points = 15, Difficulty = "Normal" },
                    new GoalItem { Id = 8, Description = "Drink 8 glasses of water", CategoryId = 3, Points = 5, Difficulty = "Easy" },
                    new GoalItem { Id = 9, Description = "Meditate for 10 minutes", CategoryId = 3, Points = 10, Difficulty = "Normal" }
                }
            },
            new Category
            {
                Id = 4,
                Name = "Finance",
                Icon = "bi-cash-coin",
                Color = "#FBBC05",
                LastCompletedDate = DateTime.Now.AddDays(-2),
                Goals = new List<GoalItem>
                {
                    new GoalItem { Id = 10, Description = "Track daily expenses", CategoryId = 4, Points = 5, Difficulty = "Easy" },
                    new GoalItem { Id = 11, Description = "Save 10% of income", CategoryId = 4, Points = 20, Difficulty = "Hard" },
                    new GoalItem { Id = 12, Description = "Review budget once a week", CategoryId = 4, Points = 10, Difficulty = "Normal" }
                }
            },
            new Category
            {
                Id = 5,
                Name = "Personal Development",
                Icon = "bi-person-plus",
                Color = "#9C27B0",
                LastCompletedDate = DateTime.Now.AddDays(-2),
                Goals = new List<GoalItem>
                {
                    new GoalItem { Id = 13, Description = "Journal for 5 minutes", CategoryId = 5, Points = 5, Difficulty = "Easy" },
                    new GoalItem { Id = 14, Description = "Practice a hobby for 15 minutes", CategoryId = 5, Points = 10, Difficulty = "Normal" },
                    new GoalItem { Id = 15, Description = "Connect with a friend or family member", CategoryId = 5, Points = 10, Difficulty = "Normal" }
                }
            }
        };

        _currentUser = new User
        {
            Id = 1,
            Username = "User",
            LastActiveDate = DateTime.Now
        };

        await SaveDataAsync();
    }

    // Get icon based on category name
    private string GetIconForCategory(string categoryName)
    {
        return categoryName.ToLower() switch
        {
            "career" => "bi-briefcase",
            "education" => "bi-book",
            "health" => "bi-heart",
            "finance" => "bi-cash-coin",
            "personal development" => "bi-person-plus",
            _ => "bi-check-circle"
        };
    }

    // Get color based on category name
    private string GetColorForCategory(string categoryName)
    {
        return categoryName.ToLower() switch
        {
            "career" => "#4285F4", // Google Blue
            "education" => "#34A853", // Google Green
            "health" => "#EA4335", // Google Red
            "finance" => "#FBBC05", // Google Yellow
            "personal development" => "#9C27B0", // Purple
            _ => "#607D8B" // Blue Grey
        };
    }

    // Save data to Azure Storage
    private async Task SaveDataAsync()
    {
        try
        {
            // Save categories to Azure Storage
            await _storageService.SaveDataAsync(CATEGORIES_FILENAME, _categories);
            
            // Save user data to Azure Storage
            await _storageService.SaveDataAsync(USER_FILENAME, _currentUser);
            
            _logger.LogInformation("Data saved to Azure Storage");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving data to Azure Storage");
            SaveToLocalStorageFallback();
        }
    }

    // Load data from Azure Storage
    private async Task LoadFromAzureStorageAsync()
    {
        try
        {
            // Load categories from Azure Storage
            var categories = await _storageService.GetDataAsync<List<Category>>(CATEGORIES_FILENAME);
            if (categories != null && categories.Count > 0)
            {
                _categories = categories;
                _logger.LogInformation("Categories loaded from Azure Storage");
            }
            
            // Load user data from Azure Storage
            var user = await _storageService.GetDataAsync<User>(USER_FILENAME);
            if (user != null)
            {
                _currentUser = user;
                _logger.LogInformation("User data loaded from Azure Storage");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading data from Azure Storage");
            LoadFromLocalStorageFallback();
        }
    }

    // Fallback methods for when Azure Storage is unavailable
    private void SaveToLocalStorageFallback()
    {
        // In a real app, we could save to browser's localStorage or IndexedDB
        _logger.LogInformation("Using local storage fallback for saving data");
    }

    private void LoadFromLocalStorageFallback()
    {
        // In a real app, we would load from browser's localStorage or IndexedDB
        _logger.LogInformation("Using local storage fallback for loading data");
    }
}