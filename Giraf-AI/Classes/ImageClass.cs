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
        public ImageText ImageText { get; init; } = new();
    }

    public class ImageText
    {
        public string TitleDa { get; init; } = string.Empty;
        public string TitleEn { get; init; } = string.Empty;
        public string AltTextDa { get; init; } = string.Empty;
        public string AltTextEn { get; init; } = string.Empty;

    }
}