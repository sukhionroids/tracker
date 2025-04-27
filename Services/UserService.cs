using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using WebApp.Models;

namespace WebApp.Services;

public class UserService
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly GoalsService _goalsService;
    private readonly ILogger<UserService> _logger;

    public UserService(
        AuthenticationStateProvider authenticationStateProvider,
        GoalsService goalsService,
        ILogger<UserService> logger)
    {
        _authenticationStateProvider = authenticationStateProvider;
        _goalsService = goalsService;
        _logger = logger;
    }

    public async Task<User> GetCurrentUserAsync()
    {
        // Get the current authentication state
        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity?.IsAuthenticated != true)
        {
            _logger.LogWarning("User is not authenticated");
            return new User();
        }

        // Get the existing user from the goals service
        var currentUser = _goalsService.GetCurrentUser();

        // Update with Azure AD information
        string objectId = user.FindFirst("oid")?.Value 
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? string.Empty;

        string email = user.FindFirst("preferred_username")?.Value 
            ?? user.FindFirst(ClaimTypes.Email)?.Value 
            ?? string.Empty;

        string name = user.FindFirst("name")?.Value 
            ?? user.FindFirst(ClaimTypes.Name)?.Value 
            ?? email.Split('@')[0];

        // Update the user with Azure AD information
        if (!string.IsNullOrEmpty(objectId))
        {
            currentUser.ObjectId = objectId;
        }

        if (!string.IsNullOrEmpty(email))
        {
            currentUser.Email = email;
        }

        // If username was not set previously, use the Azure AD name
        if (string.IsNullOrEmpty(currentUser.Username) || currentUser.Username == "User")
        {
            currentUser.Username = name;
        }

        // Save the updated user
        _goalsService.UpdateUser(currentUser);

        return currentUser;
    }

    public async Task<Dictionary<string, string>> GetUserClaimsAsync()
    {
        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        var claims = new Dictionary<string, string>();

        foreach (var claim in authState.User.Claims)
        {
            claims[claim.Type] = claim.Value;
        }

        return claims;
    }
}