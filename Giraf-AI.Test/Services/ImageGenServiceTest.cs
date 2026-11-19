using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Giraf_AI.DTOs.Providers.ImageProvider;
using Giraf_AI.Test.TestHelpers;

namespace Giraf_AI.Test.Services;

public class ImageGenServiceTest
{
    
    [Fact]
    public async Task GenerateImageData_ReturnsImageData()
    {
        // Arrange
        var providerResponse = new ImageProviderResponse
        {
            Data =
            [
                new ImageProviderData
                {
                    B64Json = "fake-base64-image-data"
                }
            ]
        };

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(providerResponse)
        };
        
        var service = ServiceFactory.CreateImageGenService(httpResponse);
        
        // Act
        var result = await service.GenerateImageData("A Cat");
        
        // Assert
        Assert.Equal("fake-base64-image-data", result);

    }
    
    [Fact]
    public async Task GenerateImageData_NullImageData_ThrowsException()
    {
        // Arrange
        var providerResponse = new ImageProviderResponse
        {
            Data =
            [
                new ImageProviderData
                {
                    B64Json = ""
                }
            ]
        };

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(providerResponse)
        };
        
        var service = ServiceFactory.CreateImageGenService(httpResponse);
        
        // Act
        var exception = await Record.ExceptionAsync(() => service.GenerateImageData("A Cat"));
        
        // Assert
        Assert.IsType<InvalidOperationException>(exception);

    }
    
    [Fact]
    public async Task GenerateImageData_EmptyImages_ThrowsException()
    {
        // Arrange
        var providerResponse = new ImageProviderResponse
        {
            Data = []
        };

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(providerResponse)
        };
        
        var service = ServiceFactory.CreateImageGenService(httpResponse);
        
        // Act
        var exception = await Record.ExceptionAsync(() => service.GenerateImageData("A Cat"));
        
        // Assert
        Assert.IsType<InvalidOperationException>(exception);

    }

    [Fact]
    public async Task GenerateImageData_EmptyResponseBody_ThrowsException()
    {
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(string.Empty)
        };
        
        var service = ServiceFactory.CreateImageGenService(httpResponse);
        
        // Act
        var exception = await Record.ExceptionAsync(() => service.GenerateImageData("A Cat"));
        
        // Assert
        Assert.IsType<JsonException>(exception);
    }

    [Fact]
    public async Task GenerateImageData_NullResponseBody_ThrowsException()
    {
        // Arrange
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null")
        };
        
        var service = ServiceFactory.CreateImageGenService(httpResponse);

        // Act
        var exception = await Record.ExceptionAsync(() => service.GenerateImageData("A Cat"));
        
        // Assert
        Assert.IsType<InvalidOperationException>(exception);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task GenerateImageData_UnsuccesfulResponse_ThrowsHttpRequestException(HttpStatusCode statusCode)
    {
        // Arrange
        var httpResponse = new HttpResponseMessage(statusCode);
        
        var service = ServiceFactory.CreateImageGenService(httpResponse);
        
        // Act
        var exception = await Record.ExceptionAsync(() => service.GenerateImageData("A Cat"));
        
        // Assert
        Assert.IsType<HttpRequestException>(exception);
    }
}