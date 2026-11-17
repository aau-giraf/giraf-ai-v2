using Giraf_AI.DTOs;
using Giraf_AI.Services;

namespace Giraf_AI.Test.Services;

public class ImageServiceMock : IImageService
{
    public Task<ImageResult> ProcessImageRequestAsync(ImageRequest request)
    {
        return Task.FromResult(new ImageResult
        {
            ImageData = "",
            TitleDa = "",
            TitleEn = "",
            ImgID = ""
        });
    }
}