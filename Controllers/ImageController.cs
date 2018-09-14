using System;
using System.Threading.Tasks;
using Amazon.S3.Model;
using ImageHost.Data;
using ImageHost.Models;
using ImageHost.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ImageHost.Controllers {
    public class ImageController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAwsHelper _awsHelper;
        private readonly ISettingsHelper _settingsHelper;
        private readonly UserManager<ApplicationUser> _userManager;

        public ImageController(
            ApplicationDbContext context,
            IAwsHelper awsHelper,
            ISettingsHelper settingsHelper,
            UserManager<ApplicationUser> userManager
        )
        {
            _context = context;
            _awsHelper = awsHelper;
            _settingsHelper = settingsHelper;
            _userManager = userManager;
        }
        
        [HttpGet]
        public async Task<IActionResult> Detail(string id)
        {
            var user = await _userManager.GetUserAsync(User);
            var image = await _context.Images
                .Include(i => i.OwnBy)
                .Include(i => i.Album)
                .SingleAsync(i => i.Id == id);

            if (!HasPermissionToImage(user, image)) return Unauthorized();
            
            if (image == null)
            {
                return NotFound();
            }

            var link = Url.Action("Direct", new { id });

            ViewBag.Image = image;
            ViewBag.Id = id;
            ViewBag.ImageLink = link;
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = image.FileSize;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1) {
                order++;
                len = len/1024;
            }

            ViewBag.ImageSize = string.Format("{0:0.##} {1}", len, sizes[order]);
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Direct(string id)
        {
            var user = await _userManager.GetUserAsync(User);
            var image = await _context.Images
                .Include(i => i.OwnBy)
                .Include(i => i.Album)
                .SingleAsync(i => i.Id == id);
                
            if (image == null) return NotFound();
            if (!HasPermissionToImage(user, image)) return Unauthorized();

            var headerValue = Request.Headers["If-Modified-Since"];
            if (!string.IsNullOrEmpty(headerValue))
            {
                var modifiedSince = DateTime.Parse(headerValue).ToLocalTime();
                if (modifiedSince >= image.UploadTimeUtc)
                {
                    return StatusCode(304);
                }
            }
            var expireTime = TimeSpan.Parse(await _settingsHelper.Get(Settings.ImageCacheTime));
            var link = (await _awsHelper.GetS3Client()).GetPreSignedURL(new GetPreSignedUrlRequest
            {
                BucketName = await _settingsHelper.Get(Settings.S3BucketName),
                Expires = DateTime.Now.AddMinutes(expireTime.TotalMinutes),
                Key = image.Id
            });

            var cacheability = image.Album.IsPrivate ? "private" : "public";
            HttpContext.Response.Headers.Add("Last-Modified", image.UploadTimeUtc.ToUniversalTime().ToString("R"));
            HttpContext.Response.Headers.Add("Cache-Control", $"{cacheability}, max-age={expireTime.TotalSeconds}");
            return Redirect(link);
        }
        
        #region Helper

        private bool HasPermissionToImage(ApplicationUser user, Image image)
        {
            if (!image.Album.IsPrivate) return true;
            
            return User.Identity.IsAuthenticated && image.OwnBy == user;
        }
        #endregion
    }
}