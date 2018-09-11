using System.Threading.Tasks;
using Amazon.S3;

namespace ImageHost.Services
{
    public interface IAwsHelper
    {
        Task<AmazonS3Client> GetS3Client();
    }
}