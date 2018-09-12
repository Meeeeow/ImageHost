using System;
using System.Threading.Tasks;
using Amazon.S3.Model;
using ImageHost.Data;
using ImageHost.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ImageHost.Controllers {
    public class ImageController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAwsHelper _awsHelper;
        private readonly ISettingsHelper _settingsHelper;

        public ImageController(
            ApplicationDbContext context,
            IAwsHelper awsHelper,
            ISettingsHelper settingsHelper
        )
        {
            _context = context;
            _awsHelper = awsHelper;
            _settingsHelper = settingsHelper;
        }
        public async Task<IActionResult> Detail(string id)
        {
            _context.Images
                .Include(i => i.OwnBy)
                .Include(i => i.Album)
                .Load();
            var image = _context.Images.Find(id);
                
            if (image == null)
            {
                return NotFound();
            }

            var link = (await _awsHelper.GetS3Client()).GetPreSignedURL(new GetPreSignedUrlRequest
            {
                BucketName = await _settingsHelper.Get(Settings.S3BucketName),
                Expires = DateTime.Now.AddMinutes(1),
                Key = image.Id
            });

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
    }
}