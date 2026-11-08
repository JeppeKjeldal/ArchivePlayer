using ArchivePlayer.Models;
using ArchivePlayer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArchivePlayer.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class SettingsController : Controller
    {
        private readonly SettingsService _settingsService;

        public SettingsController(SettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var settings = _settingsService.LoadSettings();
            return View(settings);
        }

        [HttpPost("save")]
        public IActionResult Save(AppSettings settings)
        {
            if (settings.TmdbApiKey != null)
            {
                settings.TmdbApiKey = settings.TmdbApiKey.Trim();
            }

            _settingsService.SaveSettings(settings);
            TempData["SuccessMessage"] = "Settings saved successfully! The app will now use your TMDB API key.";
            return RedirectToAction("Index");
        }

        [HttpPost("clear")]
        public IActionResult Clear()
        {
            _settingsService.SaveSettings(new AppSettings());
            TempData["SuccessMessage"] = "TMDB API key cleared. The app will now run without TMDB metadata.";
            return RedirectToAction("Index");
        }
    }
}