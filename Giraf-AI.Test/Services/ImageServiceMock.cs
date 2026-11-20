using Giraf_AI.DTOs;
using Giraf_AI.Services;

namespace Giraf_AI.Test.Services;

public class ImageServiceMock : IImageService
{
    public Task<ImageResult> ProcessImageRequestAsync(ImageRequest request)
    {
        return Task.FromResult(new ImageResult
        {
            ImageData = "image-data-base64-encoded",
            ImgID = request.ImgID,
            TitleDa = "Dansk titel",
            TitleEn = "English title",
            CitizenID = 1234
        });
    }
}