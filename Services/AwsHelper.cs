using System;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.S3;
using ImageHost.Data;

namespace ImageHost.Services
{
    public class AwsHelper : IAwsHelper
    {
        private readonly ISettingsHelper _settingsHelper;

        public AwsHelper(ISettingsHelper settingsHelper)
        {
            _settingsHelper = settingsHelper;
        }

        public async Task<AmazonS3Client> GetS3Client()
        {
            // TODO: Remove or implement proxy setting after development
            Amazon.AWSConfigs.ProxyConfig.Host = "socks5://127.0.0.1";
            Amazon.AWSConfigs.ProxyConfig.Port = 1082;
            var store = new CredentialProfileStoreChain();
            var activeAwsProfileName = await _settingsHelper.Get(Settings.AwsActiveProfile);
            if (string.IsNullOrEmpty(activeAwsProfileName))
            {
                throw new Exception("No active aws profile was set.");
            }
            store.TryGetProfile(activeAwsProfileName, out var profile);
            store.TryGetAWSCredentials(activeAwsProfileName, out var awsCredentials);
            AmazonS3Client s3 = new AmazonS3Client(awsCredentials, profile.Region);
            return s3;
        }
    }
}