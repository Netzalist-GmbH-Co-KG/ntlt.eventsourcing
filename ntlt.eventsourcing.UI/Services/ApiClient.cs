using System.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ntlt.eventsourcing.UI.Services;

public class ApiClient
{
    private readonly HttpClient _httpClient;
    private readonly SessionService _sessionService;
    private readonly ToastService _toastService;
    private readonly ILogger<ApiClient> _logger;

    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new CamelCaseNamingStrategy()
        }
    };
    
    private static readonly JsonSerializerSettings JsonSettingsSnake = new()
    {
        ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new SnakeCaseNamingStrategy()
        }
    };

    public ApiClient(HttpClient httpClient, SessionService sessionService, ToastService toastService, ILogger<ApiClient> logger)
    {
        _httpClient = httpClient;
        _sessionService = sessionService;
        _toastService = toastService;
        _logger = logger;
    }

    private void SetAuthHeader()
    {
        if (_sessionService.HasSession)
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _sessionService.BearerToken);
        }
    }

    // Session Management
    public async Task<bool> CreateSessionAsync()
    {
        try
        {
            _logger.LogInformation("Creating session - POST {Url}", "api/auth/token");

            var response = await _httpClient.PostAsync("api/auth/token", null);

            _logger.LogInformation("Response Status: {StatusCode}", response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Response Body: {Body}", responseBody);

                var result = JsonConvert.DeserializeObject<TokenResponse>(responseBody, JsonSettingsSnake);

                _logger.LogInformation("Deserialized Result: AccessToken={AccessToken}, TokenType={TokenType}",
                    result?.AccessToken, result?.TokenType);

                if (result?.AccessToken != null)
                {
                    _sessionService.BearerToken = result.AccessToken;
                    _logger.LogInformation("Session created successfully with token: {Token}", result.AccessToken[..8] + "...");
                    _toastService.ShowSuccess("Session created successfully");
                    return true;
                }
                else
                {
                    _logger.LogWarning("AccessToken was null in response");
                }
            }

            _logger.LogError("Failed to create session - Status: {Status}", response.StatusCode);
            _toastService.ShowError("Failed to create session");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during CreateSessionAsync");
            _toastService.ShowError($"Error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> EndSpecificSessionAsync(Guid sessionId)
    {
        try
        {
            _logger.LogInformation("Ending session: {SessionId}", sessionId);
            SetAuthHeader();

            // Note: This uses the current user's session to end another session
            // The API endpoint accepts the session to be ended in the body
            var response = await _httpClient.PostAsync("api/v1/cmd/session/end", JsonContent.Create(new { sessionToEndId = sessionId, reason = "User Request" }));

            if (response.IsSuccessStatusCode)
            {
                _toastService.ShowSuccess("Session ended successfully");
                if(_sessionService.SessionId == sessionId)
                    _sessionService.ClearSession();
                return true;
            }

            _logger.LogError("Failed to end session {SessionId}: {Status}", sessionId, response.StatusCode);
            _toastService.ShowError("Failed to end session");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during EndSpecificSessionAsync");
            _toastService.ShowError($"Error: {ex.Message}");
            return false;
        }
    }

    // User Commands
    public async Task<Guid?> CreateUserAsync(string userName, string email)
    {
        try
        {
            _logger.LogInformation("Creating user: {UserName}, {Email}", userName, email);
            SetAuthHeader();
            var response = await _httpClient.PostAsJsonAsync("api/v1/cmd/user/create",
                new { userName, email });

            _logger.LogInformation("CreateUser Response Status: {StatusCode}", response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("CreateUser Response Body: {Body}", responseBody);

                var result = JsonConvert.DeserializeObject<CreateUserResponse>(responseBody, JsonSettings);

                _toastService.ShowSuccess($"User '{userName}' created successfully");
                return result?.UserId;
            }

            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to create user: {Error}", error);
            _toastService.ShowError($"Failed to create user: {error}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during CreateUserAsync");
            _toastService.ShowError($"Error: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> AddPasswordAsync(Guid userId, string password)
    {
        try
        {
            SetAuthHeader();
            var response = await _httpClient.PostAsJsonAsync("api/v1/cmd/user/add-password-authentication",
                new { userId, password });

            if (response.IsSuccessStatusCode)
            {
                _toastService.ShowSuccess("Password added successfully");
                return true;
            }

            _toastService.ShowError("Failed to add password");
            return false;
        }
        catch (Exception ex)
        {
            _toastService.ShowError($"Error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> ChangeEmailAsync(Guid userId, string newEmail)
    {
        try
        {
            SetAuthHeader();
            var response = await _httpClient.PostAsJsonAsync("api/v1/cmd/user/change-email",
                new { userId, newEmail });

            if (response.IsSuccessStatusCode)
            {
                _toastService.ShowSuccess("Email changed successfully");
                return true;
            }

            _toastService.ShowError("Failed to change email");
            return false;
        }
        catch (Exception ex)
        {
            _toastService.ShowError($"Error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeactivateUserAsync(Guid userId)
    {
        try
        {
            SetAuthHeader();
            var response = await _httpClient.PostAsJsonAsync("api/v1/cmd/user/deactivate",
                new { userId });

            if (response.IsSuccessStatusCode)
            {
                _toastService.ShowSuccess("User deactivated");
                return true;
            }

            _toastService.ShowError("Failed to deactivate user");
            return false;
        }
        catch (Exception ex)
        {
            _toastService.ShowError($"Error: {ex.Message}");
            return false;
        }
    }

    // Queries
    public async Task<List<UserDto>> GetUsersAsync()
    {
        try
        {
            SetAuthHeader();
            var response = await _httpClient.GetAsync("api/v1/qry/user/list");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var users = JsonConvert.DeserializeObject<List<UserDto>>(json, JsonSettings);
                return users ?? new List<UserDto>();
            }
            return new List<UserDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get users");
            return new List<UserDto>();
        }
    }

    public async Task<List<SessionDto>> GetSessionsAsync()
    {
        try
        {
            SetAuthHeader();
            var response = await _httpClient.GetAsync("api/v1/qry/session/list");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var sessions = JsonConvert.DeserializeObject<List<SessionDto>>(json, JsonSettings);
                return sessions ?? new List<SessionDto>();
            }
            return new List<SessionDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get sessions");
            return new List<SessionDto>();
        }
    }
}

// DTOs
public record TokenResponse(string AccessToken, string TokenType, int ExpiresIn);
public record CreateUserResponse(Guid UserId);
public record UserDto(Guid UserId, string UserName, string Email, bool IsDeactivated, bool HasPassword);
public record SessionDto(Guid SessionId, DateTime CreatedAt, DateTime LastAccessedAt, bool Closed);
