using System.Threading;
using System.Threading.Tasks;
using Fusonic.Extensions.MediatR;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HangfireCqrsOutbox.Handlers
{
    public record UserRegistered(int Id, string Forename, string Surname, string Email) : INotification;

    [OutOfBand]
    public class SendEmailAfterUserRegistered : INotificationHandler<UserRegistered>
    {
        private readonly ILogger<SendEmailAfterUserRegistered> logger;

        public SendEmailAfterUserRegistered(ILogger<SendEmailAfterUserRegistered> logger)
            => this.logger = logger;

        public Task Handle(UserRegistered notification, CancellationToken cancellationToken)
        {
            logger.LogInformation($"Sending email to {notification.Forename} {notification.Surname} ({notification.Email})");
            return Task.CompletedTask;
        }
    }
}