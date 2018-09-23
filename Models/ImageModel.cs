using System;
using System.ComponentModel.DataAnnotations;

namespace ImageHost.Models
{
    public class Image
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime UploadTimeUtc { get; set; } = DateTime.UtcNow;
        
        [Required]
        public string Name { get; set; }
        
        [Required]
        public string MimeType { get; set; }
        
        [Required]
        public string Sha1 { get; set; }
        
        [Required]
        public long FileSize { get; set; }
        
        public ApplicationUser OwnBy { get; set; }
        
        public Album Album { get; set; }
        
        public bool HasThumbnail { get; set; }
        public bool Compressed { get; set; }
    }
}