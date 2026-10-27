using System.Text.Json;
using System.Text.Json.Serialization;

namespace ArchivePlayer.Services
{
    public class TMDBService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _imageBaseUrl = "https://image.tmdb.org/t/p/w500";

        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public TMDBService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["TMDB:ApiKey"] ?? throw new Exception("TMDB API Key not configured");
        }

        public async Task<(string posterUrl, string tmdbTitle, int? tmdbId, string mediaType)?> SearchByTitleAsync(string title)
        {
            try
            {
                var cleanTitle = title;

                cleanTitle = System.Text.RegularExpressions.Regex.Replace(cleanTitle, @"[-_]?dktale.*", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();

                cleanTitle = System.Text.RegularExpressions.Regex.Replace(cleanTitle, @"[_]\d+$", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();

                cleanTitle = System.Text.RegularExpressions.Regex.Replace(cleanTitle, @"\s*\([^)]*\)", "").Trim();

                cleanTitle = cleanTitle.Replace("-", " ").Replace("_", " ").Trim();

                cleanTitle = System.Text.RegularExpressions.Regex.Replace(cleanTitle, @"\s*-\s*S\d+E\d+.*", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();
                cleanTitle = System.Text.RegularExpressions.Regex.Replace(cleanTitle, @"\s*\d+x\d+.*", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();

                cleanTitle = System.Text.RegularExpressions.Regex.Replace(cleanTitle, @"\s+", " ").Trim();

                Console.WriteLine($"[TMDB] Searching for cleaned title: '{cleanTitle}' (Original: '{title}')");

                var url = $"https://api.themoviedb.org/3/search/multi?api_key={_apiKey}&query={Uri.EscapeDataString(cleanTitle)}&include_adult=false";

                var response = await _httpClient.GetAsync(url);
                var rawJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode) return null;

                var result = JsonSerializer.Deserialize<TMDBSearchResult>(rawJson, _jsonOptions);

                if (result?.Results != null && result.Results.Any())
                {
                    var bestMatch = result.Results.FirstOrDefault(r => r.MediaType == "tv" || r.MediaType == "movie")
                                    ?? result.Results.First();

                    var posterUrl = string.IsNullOrEmpty(bestMatch.PosterPath)
                        ? null
                        : $"{_imageBaseUrl}{bestMatch.PosterPath}";

                    var finalTitle = bestMatch.Title ?? bestMatch.Name ?? cleanTitle;

                    return (posterUrl, finalTitle, bestMatch.Id, bestMatch.MediaType);
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<TMDBEpisodeInfo>?> GetEpisodesAsync(int tvId)
        {
            try
            {
                var url = $"https://api.themoviedb.org/3/tv/{tvId}?api_key={_apiKey}&append_to_response=external_ids";
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return null;

                var rawJson = await response.Content.ReadAsStringAsync();
                var details = JsonSerializer.Deserialize<TMDBTvDetails>(rawJson, _jsonOptions);

                if (details?.Seasons == null) return null;

                var episodes = new List<TMDBEpisodeInfo>();

                foreach (var season in details.Seasons.Where(s => s.SeasonNumber > 0))
                {
                    var seasonUrl = $"https://api.themoviedb.org/3/tv/{tvId}/season/{season.SeasonNumber}?api_key={_apiKey}";
                    var seasonResponse = await _httpClient.GetAsync(seasonUrl);
                    if (!seasonResponse.IsSuccessStatusCode) continue;

                    var seasonJson = await seasonResponse.Content.ReadAsStringAsync();
                    var seasonDetails = JsonSerializer.Deserialize<TMDBSeasonDetails>(seasonJson, _jsonOptions);

                    if (seasonDetails?.Episodes != null)
                    {
                        foreach (var episode in seasonDetails.Episodes)
                        {
                            episodes.Add(new TMDBEpisodeInfo
                            {
                                SeasonNumber = season.SeasonNumber,
                                EpisodeNumber = episode.EpisodeNumber,
                                Name = episode.Name,
                                StillUrl = string.IsNullOrEmpty(episode.StillPath)
                                    ? null
                                    : $"https://image.tmdb.org/t/p/w300{episode.StillPath}",
                                Overview = episode.Overview
                            });
                        }
                    }
                }
                return episodes;
            }
            catch
            {
                return null;
            }
        }
    }

    public class TMDBSearchResult { [JsonPropertyName("results")] public List<TMDBResult>? Results { get; set; } }
    public class TMDBResult { [JsonPropertyName("title")] public string? Title { get; set; } [JsonPropertyName("name")] public string? Name { get; set; } [JsonPropertyName("poster_path")] public string? PosterPath { get; set; } [JsonPropertyName("media_type")] public string? MediaType { get; set; } [JsonPropertyName("id")] public int Id { get; set; } }
    public class TMDBTvDetails { [JsonPropertyName("seasons")] public List<TMDBSeason>? Seasons { get; set; } }
    public class TMDBSeason { [JsonPropertyName("season_number")] public int SeasonNumber { get; set; } }
    public class TMDBSeasonDetails { [JsonPropertyName("episodes")] public List<TMDBEpisode>? Episodes { get; set; } }
    public class TMDBEpisode
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
    public class TMDBEpisodeInfo
    {
        public int SeasonNumber { get; set; }
        public int EpisodeNumber { get; set; }
        public string? Name { get; set; }
        public string? StillUrl { get; set; }
        public string? Overview { get; set; }
    }
}