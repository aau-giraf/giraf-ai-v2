namespace DefaultNamespace;

public class UserPreferencesClass
{
    public class ImagePreferences
    {
        public int Style { get; init; }
        public bool Color { get; init; }
        public bool BackgroundColor { get; init; }
    }
    
    public class TtsPreferences
    {
        public string Language { get; init; }
        public string? Accent { get; init; }
        public char Gender { get; init; }
    }
}