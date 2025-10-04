namespace MartenAkkaTests.Api.SessionManagement;

public record Session(Guid SessionId, DateTime CreatedAt, DateTime LastAccessedAt, bool Closed);