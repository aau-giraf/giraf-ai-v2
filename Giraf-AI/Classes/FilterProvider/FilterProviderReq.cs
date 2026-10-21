namespace Giraf_AI.Classes.FilterProvider;

public class FilterProviderReq
{
    public string Model { get; init; } = string.Empty;
    public List<FilterProviderData> Messages { get; init; } = [];
}