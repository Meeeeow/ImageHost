using System;
using System.Collections.Generic;
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
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly string _bucketName;
        private readonly TimeSpan _imageCacheTime;

        public ImageController(
            ApplicationDbContext context,
            IAwsHelper awsHelper,
            ISettingsHelper settingsHelper,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager
        )
        {
            _context = context;
            _awsHelper = awsHelper;
            _settingsHelper = settingsHelper;
            _userManager = userManager;
            _signInManager = signInManager;
            _bucketName = _settingsHelper.Get(Settings.S3BucketName).GetAwaiter().GetResult();
            _imageCacheTime = TimeSpan.Parse(_settingsHelper.Get(Settings.ImageCacheTime).GetAwaiter().GetResult());
        }
        
        [HttpGet]
        public async Task<IActionResult> Detail(string id)
        {
            var user = await _userManager.GetUserAsync(User);
            var image = await _context.Images
                .Include(i => i.OwnBy)
                .Include(i => i.Album)
                .SingleAsync(i => i.Id == id);

            if (!await HasPermissionToImage(image)) return Unauthorized();
            
            if (image == null)
            {
                return NotFound();
            }

            var link = Url.Action("Direct", new { id });

            ViewBag.Image = image;
            ViewBag.Id = id;
            ViewBag.ImageLink = link;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Direct(string id, bool? thumbnail)
        {
            var image = await _context.Images
                .Include(i => i.OwnBy)
                .Include(i => i.Album)
                .SingleAsync(i => i.Id == id);
                
            if (image == null) return NotFound();
            if (!await HasPermissionToImage(image)) return Unauthorized();

            var headerValue = Request.Headers["If-Modified-Since"];
            if (!string.IsNullOrEmpty(headerValue))
            {
                var modifiedSince = DateTime.Parse(headerValue).ToLocalTime();
                if (modifiedSince >= image.UploadTimeUtc)
                {
                    return StatusCode(304);
                }
            }
            var s3ImagePath = image.Id;
            if (thumbnail == true && image.HasThumbnail)
            {
                s3ImagePath = $"thumbnail/{image.Id}";
            }
            var link = (await _awsHelper.GetS3Client()).GetPreSignedURL(new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Expires = DateTime.Now.AddMinutes(_imageCacheTime.TotalMinutes),
                Key = s3ImagePath
            });

            var cacheability = image.Album.IsPrivate ? "private" : "public";
            HttpContext.Response.Headers.Add("Last-Modified", image.UploadTimeUtc.ToUniversalTime().ToString("R"));
            HttpContext.Response.Headers.Add("Cache-Control", $"{cacheability}, max-age={_imageCacheTime.TotalSeconds}");
            return Redirect(link);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(string id)
        {
            if (!_signInManager.IsSignedIn(User)) return Unauthorized();
            
            var image = await _context.Images
                .Include(i => i.OwnBy)
                .Include(i => i.Album)
                .SingleAsync(i => i.Id == id);
                
            if (image == null) return NotFound();
            if (!await HasPermissionToImage(image)) return Unauthorized();

            var objects = new List<KeyVersion>
            {
                new KeyVersion { Key = id}
            };
            if (image.HasThumbnail) objects.Add(new KeyVersion { Key = $"thumbnail/{id}" });
            
            var s3 = await _awsHelper.GetS3Client();
            await s3.DeleteObjectsAsync(new DeleteObjectsRequest
            {
                BucketName = _bucketName,
                Objects = objects
            });

            _context.Images.Remove(image);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Detail), nameof(Album), new {id = image.Album.Id});
        }

        [HttpGet]
        public async Task<IActionResult> Proxy(string id)
        {
            var image = await _context.Images
                .Include(i => i.OwnBy)
                .Include(i => i.Album)
                .SingleAsync(i => i.Id == id);
                
            if (image == null) return NotFound();
            if (!await HasPermissionToImage(image)) return Unauthorized();

            var headerValue = Request.Headers["If-Modified-Since"];
            if (!string.IsNullOrEmpty(headerValue))
            {
                var modifiedSince = DateTime.Parse(headerValue).ToLocalTime();
                if (modifiedSince >= image.UploadTimeUtc)
                {
                    return StatusCode(304);
                }
            }
            var response = await (await _awsHelper.GetS3Client()).GetObjectAsync(new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = image.Id
            });

            var cacheability = image.Album.IsPrivate ? "private" : "public";
            HttpContext.Response.Headers.Add("Last-Modified", image.UploadTimeUtc.ToString("R"));
            HttpContext.Response.Headers.Add("Cache-Control", $"{cacheability}, max-age={_imageCacheTime.TotalSeconds}");
            return File(response.ResponseStream, image.MimeType);
        }
        
        
        #region Helper

        private async Task<bool> HasPermissionToImage(Image image)
        {
            var user = await _userManager.GetUserAsync(User);
            
            if (!image.Album.IsPrivate) return true;
            
            return _signInManager.IsSignedIn(User) && image.OwnBy == user;
        }
        #endregion
    }
}