using Giraf_AI.DTOs;
using Giraf_AI.Models;
using Giraf_AI.Services;

namespace Giraf_AI.Test.Services;

public class ImageServiceMock : IImageService
{
    public Task<ImageResult> ProcessImageRequestAsync(ImageRequest request, Action onContentCheckPassed)
    {
        onContentCheckPassed();
        return Task.FromResult(new ImageResult
        {
            ImageData = "image-data-base64-encoded",
            ImgId = request.ImgID,
            TitleDa = "Dansk titel",
            TitleEn = "English title",
            CitizenId = request.MapID,
            OriginApp = request.OriginApp,
        });
    }
}