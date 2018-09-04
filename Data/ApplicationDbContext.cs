using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ImageHost.Models;

namespace ImageHost.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        
        public DbSet<Setting> Settings { get; set; }
        public DbSet<Image> Images { get; set; }
        public DbSet<Album> Albums { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<Album>()
                .HasMany(a => a.Images)
                .WithOne(i => i.Album);

            builder.Entity<Album>()
                .HasOne(a => a.CoverImage);
            
            base.OnModelCreating(builder);
        }
    }
}