using HangfireCqrsOutbox.Domain;
using Microsoft.EntityFrameworkCore;

namespace HangfireCqrsOutbox.Data;

public class UserContext(DbContextOptions<UserContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
}
