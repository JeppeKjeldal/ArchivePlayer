using ArchivePlayer.Data;
using ArchivePlayer.Models;
using ArchivePlayer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ArchivePlayer.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class WatchController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly TMDBService _tmdbService;
        private readonly UserManager<ApplicationUser> _userManager;

        public WatchController(
            ApplicationDbContext context,
            HttpClient httpClient,
            TMDBService tmdbService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _httpClient = httpClient;
            _tmdbService = tmdbService;
            _userManager = userManager;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Index(string id)
        {
            var mediaItem = await _context.MediaItems
                .FirstOrDefaultAsync(m => m.Identifier == id);

            if (mediaItem == null)
            {
                return NotFound();
            }

            var videoFiles = await GetAvailableVideoFilesAsync(id);
            ViewBag.VideoFiles = videoFiles;

            List<TmdbEpisodeInfo>? episodeThumbnails = null;
            if (mediaItem.TMDBId.HasValue && mediaItem.TMDBMediaType == "tv")
            {
                episodeThumbnails = await _tmdbService.FetchEpisodesAsync(mediaItem.TMDBId.Value);
            }
            ViewBag.EpisodeThumbnails = episodeThumbnails;

            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                var userMediaItem = await _context.UserMediaItems
                    .FirstOrDefaultAsync(umi => umi.UserId == user.Id && umi.MediaItemId == mediaItem.Id);

                if (userMediaItem != null)
                {
                    ViewBag.WatchProgressSeconds = userMediaItem.WatchProgressSeconds;
                    ViewBag.CurrentEpisode = userMediaItem.CurrentEpisodeFileName;

                    userMediaItem.LastWatched = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }

            Console.WriteLine($"[DEBUG] Watch page for {id}: {videoFiles.Count} video file(s) found");

            return View(mediaItem);
        }

        [HttpPost("SaveProgress")]
        public async Task<IActionResult> SaveProgress(string identifier, int progressSeconds, string? episodeFileName)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var mediaItem = await _context.MediaItems
                .FirstOrDefaultAsync(m => m.Identifier == identifier);

            if (mediaItem == null) return NotFound();

            var userMediaItem = await _context.UserMediaItems
                .FirstOrDefaultAsync(umi => umi.UserId == user.Id && umi.MediaItemId == mediaItem.Id);

            if (userMediaItem != null)
            {
                userMediaItem.WatchProgressSeconds = progressSeconds;
                userMediaItem.CurrentEpisodeFileName = episodeFileName;
                userMediaItem.LastWatched = DateTime.UtcNow;

                await _context.SaveChangesAsync();
            }

            return Ok();
        }

        private async Task<List<string>> GetAvailableVideoFilesAsync(string identifier)
        {
            var videoFiles = new List<string>();

            try
            {
                var metadataUrl = $"https://archive.org/metadata/{identifier}";
                var response = await _httpClient.GetAsync(metadataUrl);

                if (!response.IsSuccessStatusCode)
                {
                    return videoFiles;
                }

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("files", out JsonElement filesArray)
                    && filesArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var file in filesArray.EnumerateArray())
                    {
                        if (file.ValueKind != JsonValueKind.Object ||
                            !file.TryGetProperty("name", out JsonElement nameElement) ||
                            nameElement.ValueKind != JsonValueKind.String)
                        {
                            continue;
                        }

                        var fileName = nameElement.GetString();

                        if (fileName != null &&
                            (fileName.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) ||
                             fileName.EndsWith(".mkv", StringComparison.OrdinalIgnoreCase)))
                        {
                            videoFiles.Add(fileName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] Failed to load video files for {identifier}: {ex.Message}");
            }

            return videoFiles;
        }
    }
}