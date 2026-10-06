namespace DefaultNamespace;

using System.ComponentModel.DataAnnotations;

public class UserPreferencesClass
{
    public class ImagePreferences
    {
        [Required]
        [Range(0, 10)]
        public int Style { get; init; }
        [Required]
        public bool Color { get; init; }
        [Required]
        public bool BackgroundColor { get; init; }
    }

    public class TtsPreferences
    {
        [Required]
        public string Language { get; init; }
        public string? Accent { get; init; }
        [Required]
        public char Gender { get; init; }
    }
}