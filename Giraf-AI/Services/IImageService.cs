using Giraf_AI.DTOs;

namespace Giraf_AI.Services;

public interface IImageService
{
    public Task<ImageResult> ProcessImageRequestAsync(ImageRequest request);
}