namespace DefaultNamespace;

using System.ComponentModel.DataAnnotations;

public class UserPreferencesClass
{
    public class ImagePreferences
    { 
        [Range(1, 5)] 
        public int Style { get; init; } = 3;
        public bool Color { get; init; } =  true;
        public bool BackgroundColor { get; init; } =  false;
    }

    public class TtsPreferences
    {
        [RegularExpression("^(danish|english)$")]
        public string Language { get; init; } = "da";
        [RegularExpression("^(british|american)$")]
        public string? Accent { get; init; } = null; // default language is danish, therefore no default accent.
        [RegularExpression("[fFmM]")]
        public char Gender { get; init; } = 'f';
    }
}