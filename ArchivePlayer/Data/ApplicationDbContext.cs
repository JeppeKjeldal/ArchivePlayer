using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ArchivePlayer.Models;

namespace ArchivePlayer.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {

    }

    public DbSet<MediaItem> MediaItems { get; set; }

    public DbSet<UserMediaItem> UserMediaItems { get; set; }
}