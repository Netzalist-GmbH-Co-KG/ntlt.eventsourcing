namespace MartenAkkaTests.Api.Controller.cmd.Requests;

public record AddPasswordAuthenticationRequest(Guid UserId, string Password);
