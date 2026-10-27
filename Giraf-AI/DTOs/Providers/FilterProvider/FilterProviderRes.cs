namespace Giraf_AI.DTOs.Providers.FilterProvider;

public class FilterProviderRes
{
    public string Prompt { get; init; } = string.Empty;
    public List<FilterProviderResult> Filters { get; init; } = [];
}