using Marten;
using MartenAkkaTests.Api.Common;
using MartenAkkaTests.Api.SessionManagement;

namespace MartenAkkaTests.Api.EventSourcing;

/// <summary>
/// Interface for command handlers that process commands and return results.
/// Handlers are registered in CommandServices and executed via Handle() method.
/// </summary>
public interface ICommandHandler<TCmd> where TCmd : ICmd
{
    Task<CommandResult> Handle(
        TCmd cmd,
        IDocumentSession session,
        Session sessionObj,
        IDateTimeProvider dateTimeProvider,
        IGuidProvider guidProvider);
}
