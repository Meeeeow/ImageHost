using System.ComponentModel.DataAnnotations;

namespace ImageHost.Models.UserCenterViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}