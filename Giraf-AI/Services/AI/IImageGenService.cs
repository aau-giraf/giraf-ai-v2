namespace Giraf_AI.Services;

public interface IImageGenService
{
    public Task<string> GenerateImageData(string prompt);
}