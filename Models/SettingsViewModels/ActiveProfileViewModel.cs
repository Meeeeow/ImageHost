using System.ComponentModel.DataAnnotations;

namespace ImageHost.Models.SettingsViewModels
{
    public class ActiveProfileViewModel
    {
        [Required]
        public string ActiveProfileName { get; set; }
    }
}