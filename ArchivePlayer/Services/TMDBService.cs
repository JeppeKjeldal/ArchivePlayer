using ArchivePlayer.Models;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ArchivePlayer.Services
{
    public class TMDBService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly SettingsService _settingsService;
        private readonly string _imageBaseUrl = "https://image.tmdb.org/t/p/w500";
        private readonly string _apiBaseUrl = "https://api.themoviedb.org/3";

        private string? _cachedApiKey;
        private bool _isCacheValid;

        public TMDBService(HttpClient httpClient, IConfiguration configuration, SettingsService settingsService)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _settingsService = settingsService;

            _settingsService.SettingsChanged += OnSettingsChanged;
        }

        /// <summary>
        /// Gets the current API key, checking user settings first, then app configuration.
        /// Results are cached and invalidated when settings change.
        /// </summary>
        private string? ApiKey
        {
            get
            {
                if (!_isCacheValid)
                {
                    _cachedApiKey = LoadApiKey();
                    _isCacheValid = true;
                }
                return _cachedApiKey;
            }
        }

        private string? LoadApiKey()
        {
            // Try user settings first (settings.json)
            var settings = _settingsService.LoadSettings();
            if (!string.IsNullOrWhiteSpace(settings.TmdbApiKey) &&
                !settings.TmdbApiKey.Equals("YOUR_API_KEY_HERE", StringComparison.OrdinalIgnoreCase))
            {
                return settings.TmdbApiKey;
            }

            // Fall back to app configuration (appsettings.json or User Secrets)
            var configKey = _configuration["TMDB:ApiKey"];
            if (!string.IsNullOrWhiteSpace(configKey) &&
                !configKey.Equals("YOUR_API_KEY_HERE", StringComparison.OrdinalIgnoreCase))
            {
                return configKey;
            }

            return null;
        }

        private void OnSettingsChanged(object? sender, EventArgs e)
        {
            _isCacheValid = false;
        }

        public bool IsConfigured => ApiKey != null;

        public async Task<TmdbMatchResult?> FindMatchAsync(string title)
        {
            if (ApiKey == null)
            {
                Console.WriteLine("[TMDB] Not configured - skipping search");
                return null;
            }

            if (string.IsNullOrWhiteSpace(title)) return null;

            var cleanedTitle = TitleCleaner.CleanForSearch(title);

            var tvResult = await SearchMultiAsync(cleanedTitle, "tv");
            if (tvResult != null) return tvResult;

            return await SearchMultiAsync(cleanedTitle, "movie");
        }

        private async Task<TmdbMatchResult?> SearchMultiAsync(string query, string mediaType)
        {
            var encodedQuery = Uri.EscapeDataString(query);
            var url = $"{_apiBaseUrl}/search/{mediaType}?api_key={ApiKey}&query={encodedQuery}&include_adult=false";

            var rawJson = await FetchRawJsonAsync(url);
            if (string.IsNullOrEmpty(rawJson)) return null;

            var response = Deserialize<TmdbSearchResponse>(rawJson);
            if (response?.Results == null || response.Results.Count == 0) return null;

            var bestMatch = response.Results[0];
            var posterUrl = !string.IsNullOrEmpty(bestMatch.PosterPath)
                ? $"{_imageBaseUrl}{bestMatch.PosterPath}"
                : null;

            return new TmdbMatchResult
            {
                TmdbId = bestMatch.Id,
                Title = bestMatch.Name ?? bestMatch.Title ?? query,
                PosterUrl = posterUrl,
                MediaType = mediaType
            };
        }

        public async Task<List<TmdbEpisodeInfo>> FetchEpisodesAsync(int tmdbId)
        {
            if (ApiKey == null)
            {
                Console.WriteLine("[TMDB] Not configured - skipping episode fetch");
                return new List<TmdbEpisodeInfo>();
            }

            var episodes = new List<TmdbEpisodeInfo>();
            var url = $"{_apiBaseUrl}/tv/{tmdbId}?api_key={ApiKey}";

            var rawJson = await FetchRawJsonAsync(url);
            if (string.IsNullOrEmpty(rawJson)) return episodes;

            var show = Deserialize<TmdbTvDetailsResponse>(rawJson);
            if (show?.Seasons == null) return episodes;

            foreach (var season in show.Seasons.Where(s => s.SeasonNumber > 0))
            {
                var seasonUrl = $"{_apiBaseUrl}/tv/{tmdbId}/season/{season.SeasonNumber}?api_key={ApiKey}";
                var seasonJson = await FetchRawJsonAsync(seasonUrl);
                if (string.IsNullOrEmpty(seasonJson)) continue;

                var seasonDetails = Deserialize<TmdbSeasonDetailsResponse>(seasonJson);
                if (seasonDetails?.Episodes == null) continue;

                foreach (var ep in seasonDetails.Episodes)
                {
                    episodes.Add(new TmdbEpisodeInfo
                    {
                        SeasonNumber = season.SeasonNumber,
                        EpisodeNumber = ep.EpisodeNumber,
                        Name = ep.Name,
                        Overview = ep.Overview,
                        StillUrl = !string.IsNullOrEmpty(ep.StillPath)
                            ? $"{_imageBaseUrl}{ep.StillPath}"
                            : null
                    });
                }
            }

            return episodes;
        }

        public async Task<List<string>> FetchGenresAsync(int tmdbId, string mediaType)
        {
            if (ApiKey == null)
            {
                Console.WriteLine("[TMDB] Not configured - skipping genre fetch");
                return new List<string>();
            }

            var url = $"{_apiBaseUrl}/{mediaType}/{tmdbId}?api_key={ApiKey}";
            var rawJson = await FetchRawJsonAsync(url);

            if (string.IsNullOrEmpty(rawJson)) return new List<string>();

            var details = Deserialize<TmdbDetailsResponse>(rawJson);

            return details?.Genres?
                .Where(g => !string.IsNullOrWhiteSpace(g.Name))
                .Select(g => g.Name!)
                .ToList() ?? new List<string>();
        }

        private async Task<string?> FetchRawJsonAsync(string url)
        {
            try
            {
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return null;
                return await response.Content.ReadAsStringAsync();
            }
            catch
            {
                return null;
            }
        }

        private T? Deserialize<T>(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch
            {
                return default;
            }
        }
    }
}