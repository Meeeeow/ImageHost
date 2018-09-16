using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
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
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly string _bucketName;

        [TempData]
        public string StatusMessage { get; set; }
        
        public AlbumController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ISettingsHelper settingsHelper,
            IAwsHelper awsHelper,
            SignInManager<ApplicationUser> signInManager
        )
        {
            _context = context;
            _userManager = userManager;
            _awsHelper = awsHelper;
            _settingsHelper = settingsHelper;
            _signInManager = signInManager;
            _bucketName = _settingsHelper.Get(Settings.S3BucketName).GetAwaiter().GetResult();
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
                IsPrivate = model.AlbumCreateViewModel.Visibility != "public",
                OwnBy = await _userManager.GetUserAsync(User)
            });

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Detail(string id)
        {
            var album = await _context.Albums
                .Include(a => a.Images)
                .SingleAsync(a => a.Id == id);

            if (album == null) return NotFound();
            if (!await HasPermissionTo(album)) return Forbid();
            
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
            var album = await _context.Albums
                .Include(a => a.Images)
                .SingleAsync(a => a.Id == albumId);

            if (album == null) return BadRequest();
            if (!await HasPermissionTo(album)) return Forbid();
            
            if (files.Count > 1)
            {
                throw new Exception("Only single file upload was supported.");
            }

            var file = files[0];
            var tempImage = System.Drawing.Image.FromStream(file.OpenReadStream());

            var hasThumbnail = tempImage.Width > 255;
            System.Drawing.Image tempThumbImage = null;
            if (hasThumbnail)
            {
                tempThumbImage = ImageToThumbnail(tempImage);
            }
            
            var hash = (new SHA1CryptoServiceProvider()).ComputeHash(file.OpenReadStream());
            var imageModel = new Image
            {
                Name = file.FileName,
                MimeType = file.ContentType,
                Sha1 = BitConverter.ToString(hash).Replace("-",""),
                FileSize = file.Length,
                OwnBy = await _userManager.GetUserAsync(User),
                Album = album,
                HasThumbnail = hasThumbnail
            };

            var dup = _context.Images.Where(image => image.Sha1 == imageModel.Sha1).ToList();

            if (dup.Count > 0)
            {
                var dupImage = dup[0];
                StatusMessage = $"Image with SHA1 \"{dupImage.Sha1}\" already exist in album \"{dupImage.Album.Name}\"";
                return RedirectToAction(nameof(Detail), new {id = albumId});
            }
            
            if (string.IsNullOrEmpty(_bucketName))
            {
                throw new Exception("No S3 bucket name was set.");
            }
            var s3 = await _awsHelper.GetS3Client();
            await s3.PutObjectAsync(new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = imageModel.Id,
                InputStream = file.OpenReadStream(),
                ContentType = imageModel.MimeType
            });
            if (hasThumbnail)
            {
                await s3.PutObjectAsync(new PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = $"thumbnail/{imageModel.Id}",
                    InputStream = ImageToStream(tempThumbImage),
                    ContentType = GetMimeTypeFromImageFormat(ImageFormat.Jpeg)
                });
            }
            _context.Images.Add(imageModel);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Detail), new { id = albumId });
        }

        [HttpGet]
        public async Task<IActionResult> SetVisibility(string visibility, string albumId)
        {
            var album = await _context.Albums
                .Include(a => a.Images)
                .SingleAsync(a => a.Id == albumId);

            if (album == null) return NotFound();
            if (!await HasPermissionTo(album)) return Forbid();

            album.IsPrivate = visibility == "private";
            _context.Albums.Update(album);
            await _context.SaveChangesAsync();

            StatusMessage = $"Album '{album.Name}' was successful set to {(album.IsPrivate ? "Private" : "Public")}";
            return RedirectToAction(nameof(Detail), new {id = albumId});
        }

        [HttpGet]
        public async Task<IActionResult> Delete(string albumId)
        {
            var album = await _context.Albums
                .Include(a => a.Images)
                .SingleAsync(a => a.Id == albumId);

            if (album == null) return NotFound();
            if (!await HasPermissionTo(album)) return Forbid();

            var imagesToDelete = new List<KeyVersion>();
            if (album.Images.Count > 0)
            {
                foreach (var image in album.Images)
                {
                    imagesToDelete.Add(new KeyVersion
                    {
                        Key = image.Id
                    });
                    if (image.HasThumbnail)
                    {
                        imagesToDelete.Add(new KeyVersion
                        {
                            Key = $"thumbnail/{image.Id}"
                        });
                    }

                    _context.Images.Remove(image);
                }

                await (await _awsHelper.GetS3Client()).DeleteObjectsAsync(new DeleteObjectsRequest
                {
                    BucketName = _bucketName,
                    Objects = imagesToDelete
                });
            }
            _context.Albums.Remove(album);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }
        
        #region Helper
        
        private async Task<bool> HasPermissionTo(Album album)
        {
            var user = await _userManager.GetUserAsync(User);
            if (!album.IsPrivate) return true;
            return _signInManager.IsSignedIn(User) && album.OwnBy == user;
        }

        private System.Drawing.Image ImageToThumbnail(System.Drawing.Image originImage)
        {
            var newHeight = originImage.Height;
            var newWidth = originImage.Width;
            if (originImage.Width > 255) {
                newWidth = 255;
                var factor = (float)newWidth / originImage.Width;
                newHeight = Convert.ToInt32(originImage.Height * factor);
            }
            
            return originImage.GetThumbnailImage(newWidth, newHeight, () => false, IntPtr.Zero);
        }

        private Stream ImageToStream(System.Drawing.Image image)
        {
            var stream = new MemoryStream();
            image.Save(stream, ImageFormat.Jpeg);
            stream.Position = 0;
            return stream;
        }

        private string GetMimeTypeFromImageFormat(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
            return codecs.First(codec => codec.FormatID == format.Guid).MimeType;
        }
        
        #endregion
    }
}