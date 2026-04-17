using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Application.Interfaces;

namespace Application.Services
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService()
        {
            var CloudName = Environment.GetEnvironmentVariable("CLOUDINARY_CLOUD_NAME");
            var ApiKey = Environment.GetEnvironmentVariable("CLOUDINARY_API_KEY");
            var ApiSecret = Environment.GetEnvironmentVariable("CLOUDINARY_API_SECRET");

            var account = new Account(CloudName, ApiKey, ApiSecret);
            _cloudinary = new Cloudinary(account);
        }

        public async Task<string> UploadImageAsync(Stream fileStream, string fileName)
        {
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, fileStream)
            };

            var result = await _cloudinary.UploadAsync(uploadParams);
            return result?.SecureUrl?.ToString() ?? throw new Exception("Image upload failed");
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string? resourceType = null)
        {
            var uploadParams = new RawUploadParams
            {
                File = new FileDescription(fileName, fileStream),
                ResourceType = resourceType ?? "auto"
            };

            var result = await _cloudinary.UploadAsync(uploadParams);
            return result?.SecureUrl?.ToString() ?? throw new Exception("File upload failed");
        }
    }
}
