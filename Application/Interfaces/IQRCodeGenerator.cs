using System;

namespace Application.Interfaces
{
    public interface IQRCodeGenerator
    {
        string GenerateQRCodeBase64(string text);
    }
}
