using System.ComponentModel.DataAnnotations;

namespace ImageHost.Models.SettingsViewModels
{
    public class AddProfileViewModel
    {
        [Required]
        [Display(Name = "Profile name")]
        public string ProfileName { get; set; }
        
        [Required]
        [Display(Name = "Region")]
        public string Region { get; set; }
        
        [Required]
        [Display(Name = "Access key")]
        public string AccessKey { get; set; }
        
        [Required]
        [Display(Name = "Secret key")]
        public string SecretKey { get; set; }
    }
}