namespace Giraf_AI.DTOs.Providers.FilterProvider;

public class FilterProviderMessage
{
    public string Role { get; init; } = "user";
    public string Content { get; init; } = string.Empty;
}