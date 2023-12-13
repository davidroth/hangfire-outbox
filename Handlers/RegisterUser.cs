using HangfireCqrsOutbox.Data;
using HangfireCqrsOutbox.Domain;
using MediatR;

namespace HangfireCqrsOutbox.Handlers;

public static class RegisterUser
{
    public record Command : IRequest
    {
        public required string Email { get; init; }
        public required string Forename { get; init; }
        public required string Surname { get; init; }
    }

    public class Handler(UserContext dbContext, IMediator mediator) : IRequestHandler<Command>
    {
        public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
        {
            var user = new User(request.Forename, request.Surname, request.Email);
            await dbContext.AddAsync(user, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            await mediator.Publish(new UserRegistered(user.Id, request.Email, request.Forename, request.Surname), cancellationToken);

            return default;
        }
    }
}
