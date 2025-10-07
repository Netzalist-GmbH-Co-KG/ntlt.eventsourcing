using FluentValidation.TestHelper;
using ntlt.eventsourcing.autx.UserManagement.Cmd;
using ntlt.eventsourcing.autx.UserManagement.Validators;

namespace ntlt.eventsourcing.autx.Tests.UserManagement.ChangeUserEmail;

public class ChangeUserEmailTests
{
    private ChangeUserEmailCmdValidator _validator = null!;

    [SetUp]
    public void Setup()
    {
        _validator = new ChangeUserEmailCmdValidator();
    }

    [Test]
    public void ChangeUserEmailCmd_WhenUserIdEmpty_ShouldHaveValidationError()
    {
        // Arrange
        var cmd = new ChangeUserEmailCmd(null, Guid.Empty, "new@example.com");

        // Act
        var result = _validator.TestValidate(cmd);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Test]
    public void ChangeUserEmailCmd_WhenEmailEmpty_ShouldHaveValidationError()
    {
        // Arrange
        var cmd = new ChangeUserEmailCmd(null, Guid.NewGuid(), "");

        // Act
        var result = _validator.TestValidate(cmd);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NewEmail)
            .WithErrorMessage("Email is required");
    }

    [Test]
    public void ChangeUserEmailCmd_WhenEmailInvalid_ShouldHaveValidationError()
    {
        // Arrange
        var cmd = new ChangeUserEmailCmd(null, Guid.NewGuid(), "not-an-email");

        // Act
        var result = _validator.TestValidate(cmd);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NewEmail)
            .WithErrorMessage("Invalid email format");
    }

    [Test]
    public void ChangeUserEmailCmd_WhenValid_ShouldNotHaveValidationError()
    {
        // Arrange
        var cmd = new ChangeUserEmailCmd(null, Guid.NewGuid(), "valid@example.com");

        // Act
        var result = _validator.TestValidate(cmd);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void UserEmailChangedEvent_ShouldHaveCorrectProperties()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var newEmail = "new@example.com";
        var timestamp = DateTime.UtcNow;

        // Act
        var evt = new UserEmailChangedEvent(sessionId, userId, newEmail, timestamp);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(evt.SessionId, Is.EqualTo(sessionId));
            Assert.That(evt.UserId, Is.EqualTo(userId));
            Assert.That(evt.NewEmail, Is.EqualTo(newEmail));
            Assert.That(evt.Timestamp, Is.EqualTo(timestamp));
        });
    }
}
