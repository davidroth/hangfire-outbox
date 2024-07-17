namespace HangfireOutbox.Handlers;

public record UserRegistered(int Id, string Forename, string Surname, string Email) : INotification;

[OutOfBand]
public class SendEmailAfterUserRegistered(ILogger<SendEmailAfterUserRegistered> logger) : INotificationHandler<UserRegistered>
{
    public Task Handle(UserRegistered notification, CancellationToken cancellationToken)
    {
        logger.LogInformation($"Sending email to {notification.Forename} {notification.Surname} ({notification.Email})");
        return Task.CompletedTask;
    }
}
