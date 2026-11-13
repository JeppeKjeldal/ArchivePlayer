using System.ComponentModel.DataAnnotations;

namespace ArchivePlayer.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Please enter a display name")]
        [StringLength(50, ErrorMessage = "Display name cannot exceed 50 characters")]
        public string DisplayName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}