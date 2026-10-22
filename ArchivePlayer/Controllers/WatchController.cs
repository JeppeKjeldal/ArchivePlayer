using ArchivePlayer.Data;
using ArchivePlayer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArchivePlayer.Controllers

{
    [Authorize]
    [Route("[controller]")]
    public class WatchController: Controller
    {
        private readonly ApplicationDbContext _context;

        public WatchController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Index(string id)
        {
            Console.WriteLine($"[DEBUG] WatchController hit! Received id: '{id}'");

            var mediaItem = await _context.MediaItems.FirstOrDefaultAsync(m => m.Identifier == id);

            if (mediaItem == null)
            {
                return NotFound();
            }

            return View(mediaItem);
        }
    }
}
