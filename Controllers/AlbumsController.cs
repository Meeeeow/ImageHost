using System.Threading.Tasks;
using ImageHost.Data;
using ImageHost.Models;
using ImageHost.Models.AlbumViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ImageHost.Controllers
{
    [Authorize]
    public class AlbumsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        
        public AlbumsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager
        )
        {
            _context = context;
            _userManager = userManager;
        }
        
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var list = await _context.Albums.ToListAsync();
            
            return View(new IndexViewModel { AlbumListViewModel = new AlbumListViewModel
            {
                Albums = await _context.Albums.ToListAsync()
            }});
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(IndexViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(Index));
            }

            await _context.Albums.AddAsync(new Album
            {
                Name = model.AlbumCreateViewModel.AlbumName,
                IsPrivate = model.AlbumCreateViewModel.IsPrivate,
                OwnBy = await _userManager.GetUserAsync(User)
            });

            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }
    }
}