using System.ComponentModel.DataAnnotations;

namespace ArchivePlayer.Models;

public class UserMediaItem
{
    public int Id { get; set; }

    public string UserId { get; set; } = null!;

    public int MediaItemId { get; set; }

    public string CategoryName { get; set; } = null!;

    public int WatchProgressSeconds { get; set; } = 0;

    public bool IsCompleted { get; set; } = false;

    public bool AutoplayEnabled { get; set; } = true;

    public DateTime? LastWatched { get; set; } = DateTime.UtcNow;

    public string? CurrentEpisodeFileName { get; set; }

    public ApplicationUser User { get; set; } = null!;

    public MediaItem MediaItem { get; set; } = null!;
}