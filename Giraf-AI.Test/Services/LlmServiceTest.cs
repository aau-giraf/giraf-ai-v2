﻿using System.Net;
 using System.Net.Http.Json;
 using System.Text.Json;
 using Giraf_AI.DTOs.Providers.LlmProvider;
using Giraf_AI.Test.TestHelpers;

namespace Giraf_AI.Test.Services;

public class LlmServiceTest
{
    [Fact]
    public async Task GenerateLlmAsync_ReturnsResult()
    {
        var path = TempFileHelper.CreateTempFile("Master prompt");

        try
        {
        var providerResponse = new LlmProviderResponse
        {
            LlmResponse = "llm-response"
        };

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(providerResponse)
        };
        
        var service = ServiceFactory.CreateLlmService(httpResponse);
        
        // Act
        var result = await service.GenerateLlmAsync("A cat", path);
        
        // Assert
        Assert.Equal(providerResponse.LlmResponse, result);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task GenerateLlmAsync_NullResponseBody()
    {
        var path = TempFileHelper.CreateTempFile("Master prompt");

        try
        {
        // Arrange
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null")
        };
        
        var service = ServiceFactory.CreateLlmService(httpResponse);
        
        // Act
        var exception = await Record.ExceptionAsync(async () => await service.GenerateLlmAsync("A cat", path));
        
        // Assert
        Assert.IsType<InvalidOperationException>(exception);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task GenerateLlmAsync_EmptyResponseBody()
    {
        var path = TempFileHelper.CreateTempFile("Master prompt");

        try
        {
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(string.Empty)
        };
        
        var service = ServiceFactory.CreateLlmService(httpResponse);
        
        var exception = await Record.ExceptionAsync(async () => await service.GenerateLlmAsync("A cat", path));
        Assert.IsType<JsonException>(exception);
        }
        finally
        {
            File.Delete(path);
        }
    }
}