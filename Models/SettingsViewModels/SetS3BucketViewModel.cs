using System.ComponentModel.DataAnnotations;

namespace ImageHost.Models.SettingsViewModels
{
    public class SetS3BucketViewModel
    {
        [Required]
        public string BucketName { get; set; }
    }
}