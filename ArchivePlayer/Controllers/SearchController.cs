using ArchivePlayer.Data;
using ArchivePlayer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ArchivePlayer.Controllers
{
    [Authorize]
    public class SearchController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly HttpClient _httpClient;



        public SearchController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, HttpClient httpClient)
        {
            _context = context;
            _userManager = userManager;
            _httpClient = httpClient;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddSource(SearchInputModel searchInputModel)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", searchInputModel);
            }

            string identifier;

            if (searchInputModel.InputString.Contains("archive.org/details/"))
            {
                identifier = searchInputModel.InputString.Split("archive.org/details/")[1].TrimEnd('/');
            }
            else
            {
                identifier = searchInputModel.InputString;
            }

            Console.WriteLine($"Parsed Identifier: {identifier}");

            await CreateNewMediaItem(identifier);

            var mediaItem = await _context.MediaItems.FirstOrDefaultAsync(m => m.Identifier == identifier);

            var user = await _userManager.GetUserAsync(User);

            await CreateNewUserMediaItem(mediaItem, user.Id);

            return RedirectToAction("Index", "Home");
        }

        private async Task CreateNewMediaItem(string identifier)
        {
            bool exists = await _context.MediaItems.AnyAsync(m => m.Identifier == identifier);

            if (!exists)
            {
                string mediaType = "Unknown";

                try
                {
                    var metadataUrl = $"https://archive.org/metadata/{identifier}";
                    var response = await _httpClient.GetAsync(metadataUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var doc = JsonDocument.Parse(json);

                        mediaType = doc.RootElement
                            .GetProperty("metadata")
                            .GetProperty("mediatype")
                            .GetString() ?? "Unknown";
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to fetch metadata for {identifier}: {ex.Message}");
                }

                var newMediaItem = new MediaItem
                {
                    Identifier = identifier,
                    Title = identifier,
                    MediaType = mediaType,
                    ThumbnailUrl = $"https://archive.org/services/img/{identifier}"
                };

                _context.MediaItems.Add(newMediaItem);
                Console.WriteLine($"[DEBUG] Saved MediaItem: {identifier} with MediaType: {mediaType}");
                await _context.SaveChangesAsync();
            }
        }

        private async Task CreateNewUserMediaItem(MediaItem? mediaItem, string userId)
        {
            var newUserMediaItem = new UserMediaItem
            {
                UserId = userId,
                MediaItemId = mediaItem!.Id,
                CategoryName = "Uncategorized"
            };

            _context.UserMediaItems.Add(newUserMediaItem);

            await _context.SaveChangesAsync();
        }
    }
}
