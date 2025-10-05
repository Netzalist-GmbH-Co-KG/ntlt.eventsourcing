using MartenAkkaTests.Api.SessionManagement;
using MartenAkkaTests.Api.SessionManagement.Cmd;
using MartenAkkaTests.Api.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace MartenAkkaTests.Api.Tests.SessionManagement.CreateSession;

public class Tests : ServiceTestBase
{
    private SessionCommandService _sut = null!;

    [SetUp]
    public void Setup()
    {
        _sut = ServiceProvider.GetRequiredService<SessionCommandService>();
    }

    [Test]
    public async Task HandleMessage_WhenCalled_ShouldCreateSession()
    {
        // Arrange
        var cmd = new CreateSessionCmd();

        // Act
        var result = await _sut.CreateSession(cmd);

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.ResultData, Is.EqualTo(FakeGuidProvider.Guid1));
        });

        // Verify event was persisted (optional - Projection-Check)
        await using var session = DocumentStore.LightweightSession();
        var createdSession = await session.LoadAsync<Session>(FakeGuidProvider.Guid1);
        Assert.Multiple(() =>
        {
            Assert.That(createdSession, Is.Not.Null);
            Assert.That(createdSession!.SessionId, Is.EqualTo(FakeGuidProvider.Guid1));
        });
    }
}