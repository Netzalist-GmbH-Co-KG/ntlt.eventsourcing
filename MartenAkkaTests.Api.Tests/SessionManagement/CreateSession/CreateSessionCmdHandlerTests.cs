using Akka.Actor;
using MartenAkkaTests.Api.SessionManagement;
using MartenAkkaTests.Api.SessionManagement.CreateSession;
using MartenAkkaTests.Api.Tests.Infrastructure;

namespace MartenAkkaTests.Api.Tests.SessionManagement.CreateSession;

public class Tests : ActorTestBase
{
    private IActorRef _sut;
    [SetUp]
    public void Setup()
    {
        _sut = ActorOf(CreateSessionCmdHandler.Prop(ServiceProvider));
    }

    [Test]
    public async Task HandleMessage_WhenCalled_ShouldCreateSession()
    {
        // Arrange
        var cmd = new CreateSessionCmd();

        // Act
        _sut.Tell(cmd);
        var result = await ExpectMsgAsync<CreateSessionResult>(TimeSpan.FromSeconds(3));

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.SessionId, Is.EqualTo(FakeGuidProvider.Guid1));
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