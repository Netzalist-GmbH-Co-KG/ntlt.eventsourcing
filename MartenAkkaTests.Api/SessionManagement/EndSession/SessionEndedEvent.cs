namespace MartenAkkaTests.Api.SessionManagement.EndSession;

public record SessionEndedEvent(Guid SessionId, string Reason, DateTime EndedAt);