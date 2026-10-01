namespace DefaultNamespace;

public class ImageClass
{
    public class ImageGenerateRequest
    {
        public string Prompt { get; init; } = string.Empty;
        public UserPreferencesClass.ImagePreferences ImagePreferences { get; init; } = new();
    }

    public class ImageGenerateResponse
    {
        public string Path { get; init; } = string.Empty;
        public string Format { get; init; } = "jpg";
    }
}