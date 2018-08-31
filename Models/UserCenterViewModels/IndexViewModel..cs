using System.ComponentModel.DataAnnotations;

namespace ImageHost.Models.UserCenterViewModels
{
    public class IndexViewModel {
        public string Username { get; set; }

        public bool IsEmailConfirmed { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public string StatusMessage { get; set; }
    }
}