using HangfireCqrsOutbox.Data;
using HangfireCqrsOutbox.Domain;
using MediatR;

namespace HangfireCqrsOutbox.Handlers;

public static class RegisterUser
{
    public record Command : IRequest
    {
        public string Email { get; init; }
        public string Forename { get; init; }
        public string Surname { get; init; }
    }

    public class Handler : IRequestHandler<Command>
    {
        private readonly UserContext dbContext;
        private readonly IMediator mediator;

        public Handler(UserContext dbContext, IMediator mediator)
        {
            this.dbContext = dbContext;
            this.mediator = mediator;
        }

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
