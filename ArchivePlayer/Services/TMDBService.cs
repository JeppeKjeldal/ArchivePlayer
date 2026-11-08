using System.Text.Json;
using ArchivePlayer.Models;

namespace ArchivePlayer.Services
{
    /// <summary>
    /// Coordinates TMDB API interactions: searching for shows/movies 
    /// and fetching episode metadata.
    /// </summary>
    public class TMDBService
    {
        private const string ImageBaseUrl = "https://image.tmdb.org/t/p/";
        private const string PosterSize = "w500";
        private const string StillSize = "w300";
        private const string ApiBaseUrl = "https://api.themoviedb.org/3";

        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly JsonSerializerOptions _jsonOptions;

        public TMDBService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["TMDB:ApiKey"]
                ?? throw new InvalidOperationException("TMDB API Key is not configured");

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<TmdbSearchMatch?> FindMatchAsync(string title)
        {
            var cleanTitle = TitleCleaner.CleanForSearch(title);
            if (string.IsNullOrEmpty(cleanTitle))
            {
                Console.WriteLine($"[TMDB] Skipping search: title '{title}' cleaned to empty string");
                return null;
            }

            Console.WriteLine($"[TMDB] Searching for cleaned title: '{cleanTitle}' (Original: '{title}')");

            var response = await _httpClient.GetAsync(BuildSearchUrl(cleanTitle));
            var rawJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[TMDB] API error ({response.StatusCode}): {rawJson}");
                return null;
            }

            var searchResult = Deserialize<TmdbSearchResponse>(rawJson);
            if (searchResult?.Results == null || !searchResult.Results.Any())
            {
                Console.WriteLine($"[TMDB] No results found for '{cleanTitle}'");
                return null;
            }

            var bestMatch = SelectBestMatch(searchResult.Results);
            return BuildMatchFromResult(bestMatch);
        }

        public async Task<List<TmdbEpisodeInfo>?> FetchEpisodesAsync(int tvId)
        {
            Console.WriteLine($"[TMDB] Fetching episodes for TV ID: {tvId}");

            var tvDetails = await FetchTvDetailsAsync(tvId);
            if (tvDetails?.Seasons == null) return null;

            var episodes = new List<TmdbEpisodeInfo>();

            foreach (var season in tvDetails.Seasons.Where(s => s.SeasonNumber > 0))
            {
                var seasonEpisodes = await FetchSeasonEpisodesAsync(tvId, season.SeasonNumber);
                episodes.AddRange(seasonEpisodes);
            }

            Console.WriteLine($"[TMDB] Found {episodes.Count} episodes for TV ID {tvId}");
            return episodes;
        }

        public async Task<List<string>> FetchGenresAsync(int tmdbId, string mediaType)
        {
            var url = $"{ApiBaseUrl}/{mediaType}/{tmdbId}?api_key={_apiKey}";
            var rawJson = await FetchRawJsonAsync(url);

            if (string.IsNullOrEmpty(rawJson)) return new List<string>();

            var details = Deserialize<TmdbDetailsResponse>(rawJson);

            return details?.Genres?
                .Where(g => !string.IsNullOrWhiteSpace(g.Name))
                .Select(g => g.Name!)
                .ToList() ?? new List<string>();
        }

        private string BuildSearchUrl(string query)
        {
            var encodedQuery = Uri.EscapeDataString(query);
            return $"{ApiBaseUrl}/search/multi?api_key={_apiKey}&query={encodedQuery}&include_adult=false";
        }

        private static TmdbSearchResult SelectBestMatch(List<TmdbSearchResult> results)
        {
            var mediaMatch = results.FirstOrDefault(r =>
                r.MediaType == "tv" || r.MediaType == "movie");

            return mediaMatch ?? results.First();
        }

        private static TmdbSearchMatch BuildMatchFromResult(TmdbSearchResult result)
        {
            var posterUrl = string.IsNullOrEmpty(result.PosterPath)
                ? null
                : $"{ImageBaseUrl}{PosterSize}{result.PosterPath}";

            var cleanTitle = result.Title ?? result.Name ?? string.Empty;

            return new TmdbSearchMatch
            {
                PosterUrl = posterUrl,
                Title = cleanTitle,
                TmdbId = result.Id,
                MediaType = result.MediaType
            };
        }

        private async Task<TmdbTvDetailsResponse?> FetchTvDetailsAsync(int tvId)
        {
            var url = $"{ApiBaseUrl}/tv/{tvId}?api_key={_apiKey}&append_to_response=external_ids";
            var rawJson = await FetchRawJsonAsync(url);
            return rawJson == null ? null : Deserialize<TmdbTvDetailsResponse>(rawJson);
        }

        private async Task<IEnumerable<TmdbEpisodeInfo>> FetchSeasonEpisodesAsync(int tvId, int seasonNumber)
        {
            var url = $"{ApiBaseUrl}/tv/{tvId}/season/{seasonNumber}?api_key={_apiKey}";
            var rawJson = await FetchRawJsonAsync(url);
            if (rawJson == null) return Enumerable.Empty<TmdbEpisodeInfo>();

            var seasonDetails = Deserialize<TmdbSeasonDetailsResponse>(rawJson);
            if (seasonDetails?.Episodes == null) return Enumerable.Empty<TmdbEpisodeInfo>();

            return seasonDetails.Episodes.Select(ep => MapToEpisodeInfo(ep, seasonNumber));
        }

        private static TmdbEpisodeInfo MapToEpisodeInfo(TmdbEpisodeResponse episode, int seasonNumber)
        {
            return new TmdbEpisodeInfo
            {
                SeasonNumber = seasonNumber,
                EpisodeNumber = episode.EpisodeNumber,
                Name = episode.Name,
                Overview = episode.Overview,
                StillUrl = string.IsNullOrEmpty(episode.StillPath)
                    ? null
                    : $"{ImageBaseUrl}{StillSize}{episode.StillPath}"
            };
        }

        private async Task<string?> FetchRawJsonAsync(string url)
        {
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadAsStringAsync();
        }

        private T? Deserialize<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
    }

    public class TmdbSearchMatch
    {
        public string? PosterUrl { get; set; }
        public string Title { get; set; } = string.Empty;
        public int TmdbId { get; set; }
        public string? MediaType { get; set; }
    }
}