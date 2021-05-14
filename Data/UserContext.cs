using HangfireCqrsOutbox.Domain;
using Microsoft.EntityFrameworkCore;

namespace HangfireCqrsOutbox.Data
{
    public class UserContext : DbContext
    {
        public UserContext(DbContextOptions<UserContext> options)
            : base(options)
        { }

        public DbSet<User> Users { get; set; }
    }
}
