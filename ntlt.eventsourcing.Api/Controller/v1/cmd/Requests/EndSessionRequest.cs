namespace ntlt.eventsourcing.Api.Controller.v1.cmd.Requests;

public record EndSessionRequest(string Reason = "UserRequest");