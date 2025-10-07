namespace ntlt.eventsourcing.autx.Controller.v1.cmd.Requests;

public record AddPasswordAuthenticationRequest(Guid UserId, string Password);