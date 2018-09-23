using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ImageHost.Models.AlbumViewModels
{
    public class UploadViewModel
    {
        [Required]
        [Display(Name = "Choose file")]
        public IFormFile File { get; set; }
        
        public bool TinifyEnabled { get; set; }
        
        [Display(Name = "Compress via Tinify if possible")]
        public bool Compress { get; set; }
    }
}