namespace Giraf_AI.DTOs.Providers.FilterProvider;

public class FilterProviderReq
{
    public string Model { get; init; } = string.Empty;
    public List<FilterProviderMessage> Messages { get; init; } = [];
}