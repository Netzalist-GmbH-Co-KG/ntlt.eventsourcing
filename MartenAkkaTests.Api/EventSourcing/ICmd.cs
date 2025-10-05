namespace MartenAkkaTests.Api.EventSourcing;

// Marker interface for commands
public interface ICmd { Guid? SessionId { get; } }
