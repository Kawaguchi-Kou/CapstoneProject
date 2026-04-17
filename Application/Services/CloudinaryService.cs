using System;
using System.IO;
using System.Threading.Tasks;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Application.Interfaces;

namespace Application.Services
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService()
        {

            var cloudName = Environment.GetEnvironmentVariable("CLOUDINARY_CLOUD_NAME");
            var apiKey = Environment.GetEnvironmentVariable("CLOUDINARY_API_KEY");
            var apiSecret = Environment.GetEnvironmentVariable("CLOUDINARY_API_SECRET");

            var account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
            _cloudinary.Api.Secure = true; 
        }

        public async Task<string> UploadImageAsync(Stream fileStream, string fileName)
        {
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, fileStream),

                Folder = "trip_planner/ads"
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            if (result.Error != null)
            {
                throw new Exception($"Cloudinary Upload Error: {result.Error.Message}");
            }

            return result?.SecureUrl?.ToString() ?? throw new Exception("Image upload failed");
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string? resourceType = null)
        {

            var uploadParams = new RawUploadParams
            {
                File = new FileDescription(fileName, fileStream)
            };


            var result = await _cloudinary.UploadAsync(uploadParams);

            if (result.Error != null)
            {
                throw new Exception($"Cloudinary File Upload Error: {result.Error.Message}");
            }

            return result?.SecureUrl?.ToString() ?? throw new Exception("File upload failed");
        }
    }
}