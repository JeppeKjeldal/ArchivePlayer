using System.ComponentModel.DataAnnotations;

namespace ArchivePlayer.Models
{
    public class SearchInputModel
    {
        [Required]
        [Display(Name = "Archive.org URL or Identifier")]
        public string? InputString { get; set; }
    }
}
