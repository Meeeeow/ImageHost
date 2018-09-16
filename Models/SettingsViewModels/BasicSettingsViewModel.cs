using System;
using System.ComponentModel.DataAnnotations;

namespace ImageHost.Models.SettingsViewModels
{
    public class BasicSettingViewModel
    {
        [Display(Name = "Image cache time")]
        public int ImageCacheMinutes { get; set; }

        public TimeSpan ImageCacheTime
        {
            get => TimeSpan.FromMinutes(ImageCacheMinutes);
            set => ImageCacheMinutes = (int)value.TotalMinutes;
        }
        
        [Display(Name = "Disable user registration")]
        public bool DisableUserRegistration { get; set; }

        public string StatusMessage { get; set; }
    }
}