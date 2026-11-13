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
    public class SearchController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly HttpClient _httpClient;
        private readonly TMDBService _tmdbService;

        public SearchController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            HttpClient httpClient,
            TMDBService tmdbService)
        {
            _context = context;
            _userManager = userManager;
            _httpClient = httpClient;
            _tmdbService = tmdbService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user != null)
            {
                var categories = await _context.UserMediaItems
                    .Where(u => u.UserId == user.Id)
                    .Select(u => u.CategoryName)
                    .Distinct()
                    .ToListAsync();

                ViewBag.ExistingCategories = categories;
            }
            else
            {
                ViewBag.ExistingCategories = new List<string>();
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddSource(SearchInputModel searchInputModel)
        {
            if (string.IsNullOrWhiteSpace(searchInputModel.CategoryName))
            {
                searchInputModel.CategoryName = "Uncategorized";
            }

            if (string.IsNullOrWhiteSpace(searchInputModel.InputString))
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    ViewBag.ExistingCategories = await _context.UserMediaItems
                        .Where(u => u.UserId == user.Id)
                        .Select(u => u.CategoryName)
                        .Distinct()
                        .ToListAsync();
                }
                ModelState.AddModelError("InputString", "Please enter an Archive.org URL or identifier");
                return View("Index", searchInputModel);
            }

            string identifier = searchInputModel.InputString.Contains("archive.org/details/")
                ? searchInputModel.InputString.Split("archive.org/details/")[1].TrimEnd('/')
                : searchInputModel.InputString;

            Console.WriteLine($"[DEBUG] Parsed Identifier: {identifier}, Category: {searchInputModel.CategoryName}");

            await CreateNewMediaItem(identifier);

            var mediaItem = await _context.MediaItems.FirstOrDefaultAsync(m => m.Identifier == identifier);
            var currentUser = await _userManager.GetUserAsync(User);

            if (mediaItem != null && currentUser != null)
            {
                await CreateNewUserMediaItem(mediaItem, currentUser.Id, searchInputModel.CategoryName);
            }

            return RedirectToAction("Index", "Home");
        }

        private async Task CreateNewMediaItem(string identifier)
        {
            Console.WriteLine($"[DEBUG] CreateNewMediaItem called for: {identifier}");

            var existingItem = await _context.MediaItems.FirstOrDefaultAsync(m => m.Identifier == identifier);

            if (existingItem == null)
            {
                Console.WriteLine($"[DEBUG] Item does not exist, fetching metadata...");

                string mediaType = "Unknown";
                string title = identifier;
                string? thumbnailUrl = $"https://archive.org/services/img/{identifier}";
                string? tmdbPosterUrl = null;
                int? tmdbId = null;
                string? tmdbMediaType = null;

                try
                {
                    var metadataUrl = $"https://archive.org/metadata/{identifier}";
                    var response = await _httpClient.GetAsync(metadataUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var doc = JsonDocument.Parse(json);
                        var metadata = doc.RootElement.GetProperty("metadata");

                        mediaType = metadata.GetProperty("mediatype").GetString() ?? "Unknown";
                        string archiveTitle = metadata.GetProperty("title").GetString() ?? identifier;
                        title = archiveTitle;

                        var tmdbResult = await _tmdbService.FindMatchAsync(archiveTitle);

                        if (tmdbResult != null)
                        {
                            tmdbPosterUrl = tmdbResult.PosterUrl;
                            title = tmdbResult.Title;
                            tmdbId = tmdbResult.TmdbId;
                            tmdbMediaType = tmdbResult.MediaType;

                            if (!string.IsNullOrEmpty(tmdbPosterUrl))
                            {
                                thumbnailUrl = tmdbPosterUrl;
                                Console.WriteLine($"[DEBUG] SUCCESS: TMDB poster and title applied!");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to fetch metadata for {identifier}: {ex.Message}");
                }

                var newMediaItem = new MediaItem
                {
                    Identifier = identifier,
                    Title = title,
                    MediaType = mediaType,
                    ThumbnailUrl = thumbnailUrl,
                    TMDBPosterUrl = tmdbPosterUrl,
                    TMDBId = tmdbId,
                    TMDBMediaType = tmdbMediaType
                };

                _context.MediaItems.Add(newMediaItem);
                await _context.SaveChangesAsync();
            }
            else
            {
                Console.WriteLine($"[DEBUG] Item already exists in database.");

                bool isTitleMessy = existingItem.Title.Contains("dktale", StringComparison.OrdinalIgnoreCase) ||
                                    existingItem.Title.Contains("_202", StringComparison.OrdinalIgnoreCase);

                if (string.IsNullOrEmpty(existingItem.TMDBPosterUrl) || isTitleMessy)
                {
                    Console.WriteLine($"[DEBUG] Existing item lacks TMDB data or has messy title. Fetching and upgrading now...");

                    var tmdbResult = await _tmdbService.FindMatchAsync(existingItem.Title);

                    if (tmdbResult != null)
                    {
                        existingItem.TMDBPosterUrl = tmdbResult.PosterUrl;
                        existingItem.Title = tmdbResult.Title;
                        existingItem.TMDBId = tmdbResult.TmdbId;
                        existingItem.TMDBMediaType = tmdbResult.MediaType;

                        if (!string.IsNullOrEmpty(existingItem.TMDBPosterUrl))
                        {
                            existingItem.ThumbnailUrl = existingItem.TMDBPosterUrl;
                        }
                    }
                    await _context.SaveChangesAsync();
                }
            }
        }

        private async Task CreateNewUserMediaItem(MediaItem? mediaItem, string userId, string categoryName)
        {
            var newUserMediaItem = new UserMediaItem
            {
                UserId = userId,
                MediaItemId = mediaItem!.Id,
                CategoryName = string.IsNullOrWhiteSpace(categoryName) ? "Uncategorized" : categoryName
            };

            _context.UserMediaItems.Add(newUserMediaItem);
            await _context.SaveChangesAsync();
        }
    }
}