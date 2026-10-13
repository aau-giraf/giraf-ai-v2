using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;

namespace DefaultNamespace;

/// <summary>
/// Registers the HTTP clients used for communicating with the AI-model containers
/// </summary>
public static class AiClientConfiguration
{
    public static IServiceCollection AddAiClients(this IServiceCollection services)
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

        /*services.AddHttpClient("PromptCorrection", client =>
        {
            client.BaseAddress = new Uri("https://localhost:/");
        });*/
        
        /*services.AddHttpClient("PromptFilter", client =>
        {
            client.BaseAddress = new Uri("https://localhost:/");
        });*/

        return services;
    }
}