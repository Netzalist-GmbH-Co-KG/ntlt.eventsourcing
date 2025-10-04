namespace MartenAkkaTests.Api.Exceptions;

public class SessionNotReadyException(string message) : Exception(message);