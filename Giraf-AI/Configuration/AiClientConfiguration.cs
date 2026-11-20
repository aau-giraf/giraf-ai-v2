// Centralizes named HTTP client defaults so provider calls share consistent timeout and resilience settings.
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
                client.Timeout = TimeSpan.FromSeconds(30);
            });
        });
        
        // AI MODELS //
        
        // Adds the image generation model as a client. Located in Strato
        services.AddHttpClient("ImageGeneration", client =>
        {
            client.BaseAddress = new Uri("http://130.225.39.133:8100/");
        });

        // Adds the language model as a client. Located in the basement
        services.AddHttpClient("Llm", client =>
        {
            client.BaseAddress = new Uri("http://172.25.11.238:8101");
        });

        // Adds the TTS Danish model as a client. Located in Strato
        services.AddHttpClient("TtsDa", (provider, client) =>
        {
            var config = provider.GetRequiredService<IConfiguration>();
            client.BaseAddress = new Uri("http://130.225.39.133:8103/");
        });
        
        // Adds the TTS English model as a client. Located in the basement
        services.AddHttpClient("TtsEn", (provider, client) =>
        {
            var config = provider.GetRequiredService<IConfiguration>();
            client.BaseAddress = new Uri("http://172.25.11.238:8102");
        });
        
        // Endpoints to send data //
        
        // Adds the GIRAF CORE endpoint for sending images.
        services.AddHttpClient("GirafCore", client =>
        {
            client.BaseAddress = new Uri("http://localhost:4500/"); // Placeholder for the GirafCore service URL, replace with actual URL
        });
        
        // Adds the VTA endpoint for TTS.
        services.AddHttpClient("TtsVta", client =>
        {
            client.BaseAddress = new Uri("http://localhost:4500/"); // Placeholder for the VTA service URL, replace with actual URL
        });
        
        // Adds the Fp endpoint for TTS.
        services.AddHttpClient("TtsFp", client =>
        {
            client.BaseAddress = new Uri("http://localhost:4500/"); // Placeholder for the Foodplanner service URL, replace with actual URL
        });
    }
}