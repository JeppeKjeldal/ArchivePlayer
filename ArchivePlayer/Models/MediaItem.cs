namespace ArchivePlayer.Models;

public class MediaItem
{
    public int Id { get; set; }

    public string Identifier { get; set; }

    public string? DirectUrl { get; set; }

    public string Title { get; set; }

    public string? Description { get; set; }

    public string? ThumbnailUrl { get; set; }

    public string MediaType { get; set; }

    public string? TMDBPosterUrl { get; set; }

    public int? TMDBId { get; set; }

    public string? TMDBMediaType { get; set; }
}