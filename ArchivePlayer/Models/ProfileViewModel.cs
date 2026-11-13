using System.ComponentModel.DataAnnotations;

namespace ArchivePlayer.Models
{
    public class ProfileViewModel
    {
        [Required(ErrorMessage = "Display name is required")]
        [StringLength(50, ErrorMessage = "Display name cannot exceed 50 characters")]
        public string DisplayName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
    }
}