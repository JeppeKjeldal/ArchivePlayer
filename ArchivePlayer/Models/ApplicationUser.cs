using Microsoft.AspNetCore.Identity;

namespace ArchivePlayer.Models;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
}