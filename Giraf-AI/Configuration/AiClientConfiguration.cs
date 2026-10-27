namespace Giraf_AI.Configuration;

/// <summary>
/// Registers the HTTP clients used for communicating with the AI-model containers
/// </summary>
public static class AiClientConfiguration
{
    public static void AddAiClients(this IServiceCollection services)
    {
        // Configure default setting for all instances of Httpclient
        services.ConfigureHttpClientDefaults(http =>
        {
            http.ConfigureHttpClient(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(300);
            });
        });
        // Adds the image generation model as a client
        services.AddHttpClient("ImageGeneration", client =>
        {
            client.BaseAddress = new Uri("http://localhost:8000/");
        });

        services.AddHttpClient("Translation", client =>
        {
            client.BaseAddress = new Uri("https://localhost:/");
        });

        services.AddHttpClient("ContentFilter", client =>
        {
            client.BaseAddress = new Uri("https://localhost:/");
        });
        
        services.AddHttpClient("Title", client =>
        {
            client.BaseAddress = new Uri("https://localhost:/");
        });
    }
}