using System.Collections.Generic;

namespace ArchivePlayer.Models
{
    public class EpisodeSelectionViewModel
    {
        public string Identifier { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public List<string> AvailableFiles { get; set; } = new List<string>();
    }
}