namespace ArchivePlayer.DTOs;

/// <summary>
/// Represents a search result from Archive.org
/// </summary>
public class ArchiveSearchResultDto
{
    public string Identifier { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string MediaType { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
}

/// <summary>
/// Response wrapper for Archive.org search with pagination
/// </summary>
public class ArchiveSearchResponse
{
    public List<ArchiveSearchResultDto> Results { get; set; } = new();
    public bool HasMoreResults { get; set; }
    public int CurrentPage { get; set; }
    public int TotalResults { get; set; }
}

/// <summary>
/// Represents a category suggestion from TMDB
/// </summary>
public class CategorySuggestionDto
{
    public string Name { get; set; } = string.Empty;
    public string Source { get; set; } = "TMDB"; // Where the suggestion came from
}