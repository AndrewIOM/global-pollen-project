using System.IO;
using GlobalPollenProject.Shared.Identity.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace GlobalPollenProject.Shared.Identity
{
    public class UserDbContext : IdentityDbContext<ApplicationUser>
    {
        public UserDbContext(DbContextOptions<UserDbContext> options)
            : base(options)
        {
        }

        public UserDbContext()
        {
            
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            optionsBuilder.UseSqlite(config.GetConnectionString("UserConnection"));
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
        }
    }
}