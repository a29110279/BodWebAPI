using Microsoft.EntityFrameworkCore;
using BodWebAPI.Models;
namespace BodWebAPI.Data  // << 記得改成你的 Namespace
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }
        public DbSet<User> Users { get; set; }

    }
}
