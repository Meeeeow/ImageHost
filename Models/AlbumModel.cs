using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ImageHost.Models
{
    public class Album
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        public string Name { get; set; }
        
        public Image CoverImage { get; set; }
        
        [Required]
        public bool IsPrivate { get; set; }
        
        [Required]
        public ApplicationUser OwnBy { get; set; }
        
        public ICollection<Image> Images { get; set; }
    }
}