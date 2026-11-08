using ArchivePlayer.Data;
using ArchivePlayer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArchivePlayer.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class WatchController : Controller
    {
        private readonly ApplicationDbContext _context;

        public WatchController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("{identifier}")]
        public async Task<IActionResult> Index(string identifier)
        {
            var mediaItem = await _context.MediaItems
                .FirstOrDefaultAsync(m => m.Identifier == identifier);

            if (mediaItem == null)
            {
                return NotFound();
            }

            ViewBag.Identifier = identifier;
            ViewBag.Title = mediaItem.Title;
            ViewBag.Description = mediaItem.Description;

            return View(mediaItem);
        }
    }
}