namespace MartenAkkaTests.UI.Services;

public class SessionService
{
    private string? _bearerToken;
    private Guid? _sessionId;

    public event Action? OnSessionChanged;

    public string? BearerToken
    {
        get => _bearerToken;
        set
        {
            _bearerToken = value;
            _sessionId = Guid.Parse(_bearerToken!);
            OnSessionChanged?.Invoke();
        }
    }

    public bool HasSession => !string.IsNullOrEmpty(_bearerToken);

    public Guid? SessionId => _sessionId;

    public void ClearSession()
    {
        _bearerToken = null;
        OnSessionChanged?.Invoke();
    }
}
