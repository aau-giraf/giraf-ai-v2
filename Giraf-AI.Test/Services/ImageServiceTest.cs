using Giraf_AI.DTOs;
using Giraf_AI.Services;
using Giraf_AI.Test.TestHelpers;

namespace Giraf_AI.Test.Services;

public class ImageServiceTest
{
    [Fact]
    public async Task ProcessImageRequestAsync_ReturnsImageResponse()
    {
        var llmServiceMock = new LlmServiceMock();
        var imageGenServiceMock = new ImageGenServiceMock();

        var service = ServiceFactory.CreateImageService(llmServiceMock, imageGenServiceMock);

        var result = await service.ProcessImageRequestAsync(new ImageRequest
        {
            Prompt = "A Cat",
            ImgID = "1234",
            IdType = "CitizenID",
            MapID = 123,
            OriginApp = "fp"
        });

        Assert.Equal("image_data_base64_encoded", result.ImageData);
        Assert.Equal("1234", result.ImgID);
        Assert.Equal("Dansk titel", result.TitleDa);
        Assert.Equal("English title", result.TitleEn);
        Assert.Equal(123, result.CitizenID);
        Assert.Null(result.ClassID);
        Assert.Null(result.OrganizationID);
    }
    
    [Fact]
    public async Task ProcessImageRequestAsync_WhenContentIsInappropriate_ThrowsViolation()
    {
        var llmServiceMock = new LlmServiceMock
        {
            ContentFilterResponse = "inappropriate"
        };
        var service = ServiceFactory.CreateImageService(llmServiceMock, new ImageGenServiceMock());

        var exception = await Record.ExceptionAsync(() =>
            service.ProcessImageRequestAsync(new ImageRequest
            {
                Prompt = "A Cat",
                ImgID = "1234",
                IdType = "CitizenID",
                MapID = 123,
                OriginApp = "fp"
            }));

        Assert.IsType<ContentFilterViolationException>(exception);
    }
    [Theory]
    [InlineData("inappropriate", true)]
    [InlineData("Inappropriate", true)]
    [InlineData("INAPPROPRIATE", true)]
    [InlineData("iappropriate", false)]
    [InlineData("", false)]
    [InlineData("appropriate", false)]
    
    public async Task ProcessImageRequestAsync_ContentFilterResponseHandling(string input, bool expectedNsfw)
    {
        var llmServiceMock = new LlmServiceMock
        {
            ContentFilterResponse = input
        };
        var service = ServiceFactory.CreateImageService(llmServiceMock, new ImageGenServiceMock());
        
        var request = new ImageRequest
        {
            Prompt = "A Cat",
            ImgID = "1234",
            IdType = "CitizenID",
            MapID = 123,
            OriginApp = "fp"
        };
        
        // Act & Assert
        if (expectedNsfw)
        {
            await Assert.ThrowsAsync<ContentFilterViolationException>(
                () => service.ProcessImageRequestAsync(request));
        }
        else
        {
            var result = await service.ProcessImageRequestAsync(request);
            Assert.NotNull(result);
        }
    }

    [Fact]
    public async Task ProcessImageRequestAsync_NullTranslationResponse_()
    {
    }

    [Fact]
    public async Task ProcessImageRequestAsync_EmptyTranslationResponse_()
    {
        var llmServiceMock = new LlmServiceMock
        {
            TranslationResponse = string.Empty
        };
        
        var service = ServiceFactory.CreateImageService(llmServiceMock, new ImageGenServiceMock());
        
        var exception = await Record.ExceptionAsync(() =>
            service.ProcessImageRequestAsync(new ImageRequest
            {
                Prompt = "A Cat",
                ImgID = "1234",
                IdType = "CitizenID",
                MapID = 123,
                OriginApp = "fp"
            }));
    }
    
    [Fact]
    public async Task ProcessImageRequestAsync_NullContentFilterResponse()
    {
        var llmServiceMock = new LlmServiceMock
        {
            ContentFilterResponse = ""
        };
        var service = ServiceFactory.CreateImageService(llmServiceMock, new ImageGenServiceMock());
        
        var exception = await Record.ExceptionAsync(() => 
            service.ProcessImageRequestAsync(new ImageRequest
        {
            Prompt = "A Cat",
            ImgID = "1234",
            IdType = "CitizenID",
            MapID = 123,
            OriginApp = "fp"
        }));
    }
    
}