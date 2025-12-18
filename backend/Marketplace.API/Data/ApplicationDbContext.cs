using Microsoft.EntityFrameworkCore;

namespace Marketplace.API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<FeedbackLog> FeedbackLogs { get; set; }
    }
}