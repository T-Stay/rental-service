using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Threading.Tasks;

namespace RentalService.Services
{
    public class S3Service
    {
        private readonly string _bucketName;
        private readonly IAmazonS3 _s3Client;

        public S3Service(IConfiguration configuration)
        {
            _bucketName = configuration["AWS:BucketName"];
            var region = Amazon.RegionEndpoint.GetBySystemName(configuration["AWS:Region"]);
            var accessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID") ?? configuration["AWS:AccessKeyId"];
            var secretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY") ?? configuration["AWS:SecretAccessKey"];
            if (!string.IsNullOrEmpty(accessKey) && !string.IsNullOrEmpty(secretKey))
            {
                _s3Client = new AmazonS3Client(accessKey, secretKey, region);
            }
            else
            {
                _s3Client = new AmazonS3Client(region);
            }
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
        {
            var uploadRequest = new TransferUtilityUploadRequest
            {
                InputStream = fileStream,
                Key = fileName,
                BucketName = _bucketName,
                ContentType = contentType
            };

            var fileTransferUtility = new TransferUtility(_s3Client);
            await fileTransferUtility.UploadAsync(uploadRequest);
            return $"https://{_bucketName}.s3.amazonaws.com/{fileName}";
        }

        public async Task DeleteFileAsync(string key)
        {
            await _s3Client.DeleteObjectAsync(_bucketName, key);
        }
    }
}
