using System.Security.Cryptography;
using Marten;
using MartenAkkaTests.Api.EventSourcing;
using MartenAkkaTests.Api.SessionManagement;
using MartenAkkaTests.Api.SessionManagement.Evt;

namespace MartenAkkaTests.Api.UserManagement.Cmd;

// Cmd
public record AddPasswordAuthenticationCmd(Guid? SessionId, Guid UserId, string Password) : ICmd;

// Events
public record PasswordAuthenticationAddedEvent(Guid SessionId, Guid UserId, string PasswordHash);

