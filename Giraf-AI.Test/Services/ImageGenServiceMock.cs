using Giraf_AI.Services;

namespace Giraf_AI.Test.Services;

public class ImageGenServiceMock : IImageGenService
{
    public Task<string> GenerateImageData(string request)
    {
        return Task.FromResult("image_data_base64_encoded");
    }
}