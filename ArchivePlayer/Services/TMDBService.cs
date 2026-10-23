using System.Text.Json;
using System.Text.RegularExpressions;

namespace ArchivePlayer.Services
{
    public class TMDBService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _imageBaseUrl = "https://image.tmdb.org/t/p/w500";

        public TMDBService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["TMDB:ApiKey"] ?? throw new Exception("TMDB API Key not configured in appsettings.json");
            Console.WriteLine($"[TMDB] Service initialized with API Key: {_apiKey.Substring(0, 5)}...");
        }

        public async Task<(string posterUrl, int? tmdbId, string mediaType)?> SearchByTitleAsync(string title)
        {
            try
            {
                var cleanTitle = System.Text.RegularExpressions.Regex.Replace(title, @"\s*\([^)]*\)", "").Trim();
                cleanTitle = System.Text.RegularExpressions.Regex.Replace(cleanTitle, @"\s*-\s*S\d+E\d+.*", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();

                Console.WriteLine($"[TMDB] Searching for cleaned title: '{cleanTitle}' (Original: '{title}')");

                var url = $"https://api.themoviedb.org/3/search/multi?api_key={_apiKey}&query={Uri.EscapeDataString(cleanTitle)}&include_adult=false";
                Console.WriteLine($"[TMDB] Request URL: {url}");

                var response = await _httpClient.GetAsync(url);
                Console.WriteLine($"[TMDB] Response Status: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[TMDB] Error Response: {errorContent}");
                }
                else
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<TMDBSearchResult>(json);

                    if (result?.Results != null && result.Results.Any())
                    {
                        // Prefer 'tv' or 'movie' over 'person' or other types
                        var bestMatch = result.Results.FirstOrDefault(r => r.MediaType == "tv" || r.MediaType == "movie")
                                        ?? result.Results.First();

                        Console.WriteLine($"[TMDB] Found match: '{bestMatch.Title ?? bestMatch.Name}' (Type: {bestMatch.MediaType})");

                        var posterUrl = string.IsNullOrEmpty(bestMatch.PosterPath)
                            ? null
                            : $"{_imageBaseUrl}{bestMatch.PosterPath}";

                        Console.WriteLine($"[TMDB] Poster URL: {posterUrl ?? "None"}");

                        return (posterUrl, bestMatch.Id, bestMatch.MediaType);
                    }
                    else
                    {
                        Console.WriteLine($"[TMDB] No results found for '{cleanTitle}'");
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TMDB] Exception during search: {ex.Message}");
                return null;
            }
        }

        public async Task<List<TMDBEpisodeInfo>?> GetEpisodesAsync(int tvId)
        {
            try
            {
                Console.WriteLine($"[TMDB] Fetching episodes for TV ID: {tvId}");
                var url = $"https://api.themoviedb.org/3/tv/{tvId}?api_key={_apiKey}&append_to_response=external_ids";

                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return null;

                var json = await response.Content.ReadAsStringAsync();
                var details = JsonSerializer.Deserialize<TMDBTvDetails>(json);

                if (details?.Seasons == null) return null;

                var episodes = new List<TMDBEpisodeInfo>();

                foreach (var season in details.Seasons.Where(s => s.SeasonNumber > 0))
                {
                    var seasonUrl = $"https://api.themoviedb.org/3/tv/{tvId}/season/{season.SeasonNumber}?api_key={_apiKey}";
                    var seasonResponse = await _httpClient.GetAsync(seasonUrl);

                    if (!seasonResponse.IsSuccessStatusCode) continue;

                    var seasonJson = await seasonResponse.Content.ReadAsStringAsync();
                    var seasonDetails = JsonSerializer.Deserialize<TMDBSeasonDetails>(seasonJson);

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
                                    : $"https://image.tmdb.org/t/p/w300{episode.StillPath}"
                            });
                        }
                    }
                }

                Console.WriteLine($"[TMDB] Found {episodes.Count} episodes for TV ID {tvId}");
                return episodes;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TMDB] Exception fetching episodes: {ex.Message}");
                return null;
            }
        }
    }

    public class TMDBSearchResult
    {
        public List<TMDBResult>? Results { get; set; }
    }

    public class TMDBResult
    {
        public string? Title { get; set; }
        public string? Name { get; set; }
        public string? PosterPath { get; set; }
        public string? MediaType { get; set; }
        public int Id { get; set; }
    }

    public class TMDBTvDetails
    {
        public List<TMDBSeason>? Seasons { get; set; }
    }

    public class TMDBSeason
    {
        public int SeasonNumber { get; set; }
    }

    public class TMDBSeasonDetails
    {
        public List<TMDBEpisode>? Episodes { get; set; }
    }

    public class TMDBEpisode
    {
        public int EpisodeNumber { get; set; }
        public string? Name { get; set; }
        public string? StillPath { get; set; }
    }

    public class TMDBEpisodeInfo
    {
        public int SeasonNumber { get; set; }
        public int EpisodeNumber { get; set; }
        public string? Name { get; set; }
        public string? StillUrl { get; set; }
    }
}