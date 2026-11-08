namespace ArchivePlayer.DTOs;

/// <summary>
/// Full media details needed for playback
/// </summary>
public class MediaDetailsDto
{
    public string Identifier { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string MediaType { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string? TmdbPosterUrl { get; set; }
    public int? TmdbId { get; set; }
    public string? TmdbMediaType { get; set; }

    // Video files available for playback
    public List<string> VideoFiles { get; set; } = new();

    // Episode metadata (if TV show)
    public List<EpisodeInfoDto>? Episodes { get; set; }

    // User's saved progress
    public int WatchProgressSeconds { get; set; }
    public string? CurrentEpisodeFileName { get; set; }
    public DateTime? LastWatched { get; set; }
}

/// <summary>
/// Episode metadata from TMDB
/// </summary>
public class EpisodeInfoDto
{
    public int SeasonNumber { get; set; }
    public int EpisodeNumber { get; set; }
    public string? Name { get; set; }
    public string? Overview { get; set; }
    public string? StillUrl { get; set; }
    public string FileName { get; set; } = string.Empty;
}

/// <summary>
/// Request model for updating watch progress
/// </summary>
public class WatchProgressUpdateRequest
{
    public string Identifier { get; set; } = string.Empty;
    public int ProgressSeconds { get; set; }
    public string? EpisodeFileName { get; set; }
}
