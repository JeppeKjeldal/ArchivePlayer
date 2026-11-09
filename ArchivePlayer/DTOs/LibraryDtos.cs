namespace ArchivePlayer.DTOs;

/// <summary>
/// Represents a media item in the user's library
/// </summary>
public class LibraryItemDto
{
    public int Id { get; set; }
    public string Identifier { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string MediaType { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string? TmdbPosterUrl { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public DateTime AddedAt { get; set; }
    public int WatchProgressSeconds { get; set; }
}

/// <summary>
/// Extends LibraryItemDto with watch progress information
/// </summary>
public class RecentlyWatchedItemDto : LibraryItemDto
{
    public int WatchProgressSeconds { get; set; }
    public string? CurrentEpisodeFileName { get; set; }
    public DateTime? LastWatched { get; set; }
    public bool IsCompleted { get; set; }
}

/// <summary>
/// Request model for adding a new item to the library
/// </summary>
public class AddToLibraryRequest
{
    public string Identifier { get; set; } = string.Empty;
    public string CategoryName { get; set; } = "Uncategorized";
}

/// <summary>
/// Response wrapper for library listings
/// </summary>
public class LibraryResponse
{
    public Dictionary<string, List<LibraryItemDto>> Categories { get; set; } = new();
    public List<string> CategoryNames { get; set; } = new();
}