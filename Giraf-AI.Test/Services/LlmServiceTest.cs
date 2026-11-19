using System.Net;
using Giraf_AI.DTOs.Providers.LlmProvider;
using Giraf_AI.Test.TestHelpers;

namespace Giraf_AI.Test.Services;

public class LlmServiceTest
{
    [Fact]
    public async Task GenerateLlmAsync_ReturnsResult()
    {
        var providerResponse = new LlmProviderResponse
        {
            LlmResponse = "llm-response"
        };

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(providerResponse.LlmResponse)
        };
        
        var service = ServiceFactory.CreateLlmService(httpResponse);
        
        // Act
        var result = await service.GenerateLlmAsync("A cat", "");
        
        // Assert
        Assert.Equal(providerResponse.LlmResponse, result);
    }

    [Fact]
    public async Task GenerateLlmAsync_NullResponseBody()
    {
        // Arrange
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null")
        };
        
        var service = ServiceFactory.CreateLlmService(httpResponse);
        
        // Act
        var exception = await Record.ExceptionAsync(async () => await service.GenerateLlmAsync("A cat", ""));
        
        // Assert
        Assert.IsType<InvalidOperationException>(exception);
    }

    [Fact]
    public async Task GenerateLlmAsync_EmptyResponseBody()
    {
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(string.Empty)
        };
        
        var service = ServiceFactory.CreateLlmService(httpResponse);
        
        var exception = await Record.ExceptionAsync(async () => await service.GenerateLlmAsync(string.Empty, ""));
        Assert.IsType<InvalidOperationException>(exception);
    }
}