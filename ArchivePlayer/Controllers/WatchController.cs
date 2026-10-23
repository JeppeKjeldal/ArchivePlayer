using ArchivePlayer.Data;
using ArchivePlayer.Models;
using Microsoft.AspNetCore.Authorization;
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

        public WatchController(ApplicationDbContext context, HttpClient httpClient)
        {
            _context = context;
            _httpClient = httpClient;
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

            Console.WriteLine($"[DEBUG] Watch page for {id}: {videoFiles.Count} video file(s) found");

            return View(mediaItem);
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
                    Console.WriteLine($"[DEBUG] Metadata request failed: {(int)response.StatusCode}");
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