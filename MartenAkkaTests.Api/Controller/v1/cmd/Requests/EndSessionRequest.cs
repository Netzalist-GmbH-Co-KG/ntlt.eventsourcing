namespace MartenAkkaTests.Api.Controller.v1.cmd.Requests;

public record EndSessionRequest(string Reason = "UserRequest");
