using System.Threading.Tasks;
using HangfireCqrsOutbox.Handlers;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HangfireCqrsOutbox.Controllers;

[ApiController]
[Route("[controller]")]
public class UsersController : ControllerBase
{
    private readonly IMediator mediator;

    public UsersController(IMediator mediator)
    {
        this.mediator = mediator;
    }

    [HttpPost("Register")]
    public async Task Register(RegisterModel model)
    {
        await mediator.Send(new RegisterUser.Command { Email = model.Email, Forename = model.Forename, Surname = model.Surname });
    }
}

public record RegisterModel
{
    public string Email { get; init; }
    public string Forename { get; init; }
    public string Surname { get; init; }
}
