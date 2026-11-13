using System.ComponentModel.DataAnnotations;

namespace ArchivePlayer.Models
{
    public class SearchInputModel
    {
        [Required(ErrorMessage = "Please enter an Archive.org URL or identifier")]
        public string InputString { get; set; } = string.Empty;

        public string CategoryName { get; set; } = string.Empty;
    }
}