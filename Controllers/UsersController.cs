﻿using HangfireOutbox.Handlers;
using Microsoft.AspNetCore.Mvc;

namespace HangfireOutbox.Controllers;

[ApiController]
[Route("[controller]")]
public class UsersController(IMediator mediator) : ControllerBase
{
    [HttpPost("Register")]
    public async Task Register(RegisterModel model)
    {
        await mediator.Send(new RegisterUser
        {
            Email = model.Email,
            Forename = model.Forename,
            Surname = model.Surname
        }, HttpContext.RequestAborted);
    }
}

public record RegisterModel
{
    public required string Email { get; init; }
    public required string Forename { get; init; }
    public required string Surname { get; init; }
}