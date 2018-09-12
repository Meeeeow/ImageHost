using System.Threading.Tasks;
using Amazon;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ImageHost.Models.SettingsViewModels;
using Amazon.Runtime.CredentialManagement;
using ImageHost.Data;
using ImageHost.Services;

namespace ImageHost.Controllers
{
    [Authorize]
    public class SettingsController : Controller
    {

        private readonly ApplicationDbContext _context;
        private readonly ISettingsHelper _settingsHelper;
        public SettingsController(
            ApplicationDbContext context,
            ISettingsHelper settingsHelper
        )
        {
            _context = context;
            _settingsHelper = settingsHelper;
        }
        
        [TempData]
        public string StatusMessage { get; set; }
        
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        #region AWS

        [HttpGet]
        public async Task<IActionResult> AwsSettings()
        {
            var awsProfiles = new CredentialProfileStoreChain().ListProfiles();
            ViewBag.Profiles = awsProfiles;
            
            return View(new AwsViewModel
            {
                StatusMessage = StatusMessage,
                ActiveProfileViewModel = new ActiveProfileViewModel
                {
                    ActiveProfileName = await _settingsHelper.Get(Settings.AwsActiveProfile)
                },
                SetS3BucketViewModel = new SetS3BucketViewModel
                {
                    BucketName = await _settingsHelper.Get(Settings.S3BucketName)
                }
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetAwsActiveProfile(AwsViewModel model)
        {
            var profileName = model.ActiveProfileViewModel.ActiveProfileName;
            await _settingsHelper.Write(Settings.AwsActiveProfile, profileName);
            StatusMessage = $"Successful set '{profileName}' as active profile";
            return RedirectToAction(nameof(AwsSettings));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetS3BucketName(AwsViewModel model)
        {
            await _settingsHelper.Write(Settings.S3BucketName, model.SetS3BucketViewModel.BucketName);

            return RedirectToAction(nameof(AwsSettings));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddAwsProfile(AwsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                StatusMessage = "Invalid input";
                return RedirectToAction(nameof(AwsSettings));
            }
            
            var options = new CredentialProfileOptions
            {
                AccessKey = model.AddProfileViewModel.AccessKey,
                SecretKey = model.AddProfileViewModel.SecretKey
            };
            var profile = new CredentialProfile(model.AddProfileViewModel.ProfileName, options);
            profile.Region = RegionEndpoint.GetBySystemName(model.AddProfileViewModel.Region);
            var netSdkFile = new NetSDKCredentialsFile();
            netSdkFile.RegisterProfile(profile);

            StatusMessage = $"Successful added profile '{model.AddProfileViewModel.ProfileName}'";
            return RedirectToAction(nameof(AwsSettings));
        }

        [HttpGet]
        public async Task<IActionResult> DeleteAwsProfile(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                StatusMessage = "Invalid Input";
                return RedirectToAction(nameof(AwsSettings));
            }

            new CredentialProfileStoreChain().UnregisterProfile(id);

            var activeProfileName = await _settingsHelper.Get(Settings.AwsActiveProfile);
            if (activeProfileName == id)
            {
                _settingsHelper.Delete(activeProfileName);
            }

            StatusMessage = $"Profile '{id}' was deleted";
            return RedirectToAction(nameof(AwsSettings));

        }
        #endregion
    }
}