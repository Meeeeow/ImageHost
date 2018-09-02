using System.ComponentModel.DataAnnotations;

namespace ImageHost.Models.SettingsViewModels
{
    public class AwsViewModel
    {
        public string StatusMessage { get; set; }

        public ActiveProfileViewModel ActiveProfileViewModel { get; set; }
        public AddProfileViewModel AddProfileViewModel { get; set; }
    }
}