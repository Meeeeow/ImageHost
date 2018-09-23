using System.ComponentModel.DataAnnotations;

namespace ImageHost.Models.SettingsViewModels
{
    public class TinifySettingsViewModel
    {
        [Display(Name = "Enable Tinify image compress")]
        public bool Enable { get; set; }

        public bool ApiKeyValid { get; set; }
        public uint? CompressedCount { get; set; }
        
        [Display(Name = "Tinify API Key")]
        public string ApiKey { get; set; }
        
        public string StatusMessage { get; set; }
    }
}