namespace DefaultNamespace;

public class TtsClass
{
    public class TtsSynthesizeRequest
    {
        public string Text { get; init; } = string.Empty;
        public UserPreferencesClass.TtsPreferences TtsPreferences { get; init; } = new();

    }

    public class TtsSynthesizeResponse
    {
        // lydfil
    }
}