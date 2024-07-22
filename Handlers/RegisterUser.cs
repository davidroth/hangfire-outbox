using HangfireOutbox.Data;
using HangfireOutbox.Domain;

namespace HangfireOutbox.Handlers;

public record RegisterUser : ICommand
{
    public required string Email { get; init; }
    public required string Forename { get; init; }
    public required string Surname { get; init; }

    public class Handler(UserContext dbContext, IMediator mediator) : IRequestHandler<RegisterUser>
    {
        public async Task<Unit> Handle(RegisterUser request, CancellationToken cancellationToken)
        {
            var user = new User(request.Forename, request.Surname, request.Email);
            await dbContext.AddAsync(user, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            await mediator.Publish(new UserRegistered(user.Id, request.Email, request.Forename, request.Surname), cancellationToken);

            return default;
        }
    }
}
