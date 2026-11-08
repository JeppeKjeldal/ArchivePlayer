using System.Text.Json.Serialization;

namespace ArchivePlayer.Models
{
    public class TmdbSearchResponse
    {
        [JsonPropertyName("results")]
        public List<TmdbSearchResult>? Results { get; set; }
    }

    public class TmdbSearchResult
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("poster_path")]
        public string? PosterPath { get; set; }

        [JsonPropertyName("media_type")]
        public string? MediaType { get; set; }
    }

    public class TmdbTvDetailsResponse
    {
        [JsonPropertyName("seasons")]
        public List<TmdbSeasonSummary>? Seasons { get; set; }
    }

    public class TmdbSeasonSummary
    {
        [JsonPropertyName("season_number")]
        public int SeasonNumber { get; set; }
    }

    public class TmdbSeasonDetailsResponse
    {
        [JsonPropertyName("episodes")]
        public List<TmdbEpisodeResponse>? Episodes { get; set; }
    }

    public class TmdbEpisodeResponse
    {
        [JsonPropertyName("episode_number")]
        public int EpisodeNumber { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("still_path")]
        public string? StillPath { get; set; }

        [JsonPropertyName("overview")]
        public string? Overview { get; set; }
    }

    public class TmdbEpisodeInfo
    {
        public int SeasonNumber { get; set; }
        public int EpisodeNumber { get; set; }
        public string? Name { get; set; }
        public string? StillUrl { get; set; }
        public string? Overview { get; set; }
    }

    public class TmdbDetailsResponse
    {
        [JsonPropertyName("genres")]
        public List<TmdbGenre>? Genres { get; set; }
    }

    public class TmdbGenre
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    public class TmdbMatchResult
    {
        public int TmdbId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? PosterUrl { get; set; }
        public string MediaType { get; set; } = string.Empty;
    }
}