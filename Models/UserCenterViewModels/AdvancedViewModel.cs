using System.ComponentModel.DataAnnotations;
namespace ImageHost.Models.UserCenterViewModels
{
    public class AdvancedViewModel
    {
        [Range(typeof(bool), "true", "true", ErrorMessage = "Please select the checkbox to confirm this danger action.")]
        public bool Confirm { get; set; }
        
        public string StatusMessage { get; set; }        
    }
}