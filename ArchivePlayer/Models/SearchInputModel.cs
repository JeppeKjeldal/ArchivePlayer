using System.ComponentModel.DataAnnotations;

namespace ArchivePlayer.Models
{
    public class SearchInputModel
    {
        [Required(ErrorMessage = "Please enter an Archive.org URL or identifier")]
        public string InputString { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter a category name")]
        public string CategoryName { get; set; } = string.Empty;
    }
}