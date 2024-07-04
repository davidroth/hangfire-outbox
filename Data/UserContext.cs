using HangfireOutbox.Domain;
using Microsoft.EntityFrameworkCore;

namespace HangfireOutbox.Data;

public class UserContext(DbContextOptions<UserContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
}
