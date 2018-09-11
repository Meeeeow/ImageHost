using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Amazon.S3.Model;
using ImageHost.Data;
using ImageHost.Models;
using ImageHost.Models.AlbumViewModels;
using ImageHost.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ImageHost.Controllers
{
    [Authorize]
    public class AlbumController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISettingsHelper _settingsHelper;
        private readonly IAwsHelper _awsHelper;
        
        [TempData]
        public string StatusMessage { get; set; }
        
        public AlbumController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ISettingsHelper settingsHelper,
            IAwsHelper awsHelper
        )
        {
            _context = context;
            _userManager = userManager;
            _awsHelper = awsHelper;
            _settingsHelper = settingsHelper;
        }
        
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var list = await _context.Albums.Where(album => album.OwnBy == user).ToListAsync();
            
            return View(new IndexViewModel { AlbumListViewModel = new AlbumListViewModel
            {
                Albums = list
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

        [HttpGet]
        public async Task<IActionResult> Detail(string id)
        {
            var album = await _context.Albums.FindAsync(id);
            return View(new DetailViewModel
            {
                Album = album,
                StatusMessage = StatusMessage
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadImage(List<IFormFile> files, string albumId)
        {
            if (files.Count > 1)
            {
                throw new Exception("Only single file upload was supported.");
            }

            var file = files[0];
            var hash = (new SHA1CryptoServiceProvider()).ComputeHash(file.OpenReadStream());
            var imageModel = new Image
            {
                Name = file.FileName,
                MimeType = file.ContentType,
                Sha1 = BitConverter.ToString(hash).Replace("-",""),
                FileSize = file.Length,
                OwnBy = await _userManager.GetUserAsync(User),
                Album = _context.Albums.Find(albumId)
            };

            var dup = _context.Images.Where(image => image.Sha1 == imageModel.Sha1).ToList();

            if (dup.Count > 0)
            {
                var dupImage = dup[0];
                StatusMessage = $"Image with SHA1 \"{dupImage.Sha1}\" already exist in album \"{dupImage.Album.Name}\"";
                return RedirectToAction(nameof(Detail), new {id = albumId});
            }
            
            var bucketName = await _settingsHelper.Get("S3BucketName");
            if (string.IsNullOrEmpty(bucketName))
            {
                throw new Exception("No S3 bucket name was set.");
            }
            var s3 = await _awsHelper.GetS3Client();
            await s3.PutObjectAsync(new PutObjectRequest
            {
                BucketName = bucketName,
                Key = imageModel.Id,
                InputStream = file.OpenReadStream()
            });
            _context.Images.Add(imageModel);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Detail), new { id = albumId });
        }
    }
}