using System.ComponentModel.DataAnnotations;
using Giraf_AI.DTOs;

namespace Giraf_AI.Test.DTOs;

public class ImageRequestTest
{
    /// Validates an object against its Data Annotation attributes
    /// and returns a list of any validation errors.
    private static List<ValidationResult> Validate(ImageRequest request)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(request);
        Validator.TryValidateObject(request, context, results, validateAllProperties: true);
        return results;
    }
    
    [Fact]
    public void ImageRequest_PassesValidation()
    {
        var dto = new ImageRequest
        {
            Prompt = "A Cat", 
            ImgID = "123", 
            MapID = 1,  
            IdType = "CitizenID"
        };
        
        var results = Validate(dto);

        Assert.Empty(results);
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Prompt_Required_FailsValidation(string prompt)
    {
        var dto = new ImageRequest
        {
            Prompt = prompt, 
            ImgID = "123", 
            MapID = 1,  
            IdType = "CitizenID"
        };
        
        var results = Validate(dto);
        
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(ImageRequest.Prompt)));
    }
    
    [Theory]
    [InlineData(1)]
    [InlineData(99)]
    [InlineData(100)]
    public void Prompt_AtOrBelowMax_IsValid(int length)
    {
        var dto = new ImageRequest
        {
            Prompt = new string('a', length),
            ImgID = "123",
            MapID = 1,
            IdType = "CitizenID"
        };

        var results = Validate(dto);

        Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(ImageRequest.Prompt)));
    }

    [Theory]
    [InlineData(101)]
    [InlineData(150)]
    public void Prompt_AboveMax_IsInvalid(int length)
    {
        var dto = new ImageRequest
        {
            Prompt = new string('a', length),
            ImgID = "123",
            MapID = 1,
            IdType = "CitizenID"
        };

        var results = Validate(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(ImageRequest.Prompt)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void ImgID_Required_FailsValidation(string imgID)
    {
        var dto = new ImageRequest
        {
            Prompt = "A Cat", 
            ImgID = imgID, 
            MapID = 1,  
            IdType = "CitizenID"
        };
        
        var results = Validate(dto);
        
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(ImageRequest.ImgID)));
    }
    
    [Theory]
    [InlineData(1)]
    [InlineData(99)]
    [InlineData(100)]
    public void ImgID_AtOrBelowMax_IsValid(int length)
    {
        var dto = new ImageRequest
        {
            Prompt = "A Cat",
            ImgID = new string('a', length),
            MapID = 1,
            IdType = "CitizenID"
        };

        var results = Validate(dto);

        Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(ImageRequest.ImgID)));
    }

    [Theory]
    [InlineData(101)]
    [InlineData(150)]
    public void ImgID_AboveMax_IsInvalid(int length)
    {
        var dto = new ImageRequest
        {
            Prompt = "A Cat",
            ImgID = new string('a', length),
            MapID = 1,
            IdType = "CitizenID"
        };

        var results = Validate(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(ImageRequest.ImgID)));
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void IdType_Required_FailsValidation(string idType)
    {
        var dto = new ImageRequest
        {
            Prompt = "A Cat", 
            ImgID = "123", 
            MapID = 1,  
            IdType = idType
        };
        
        var results = Validate(dto);
        
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(ImageRequest.IdType)));
    }
    
    [Theory]
    [InlineData("CitizenID")]
    [InlineData("citizenid")]
    [InlineData("CITIZENID")]
    [InlineData("ClassID")]
    [InlineData("classid")]
    [InlineData("CLASSID")]
    [InlineData("OrganizationID")]
    [InlineData("organizationid")]
    [InlineData("ORGANIZATIONID")]
    public void IdType_AllowedTypes_PassesValidation(string idType)
    {
        var dto = new ImageRequest
        {
            Prompt = "A Cat",
            ImgID = "123",
            MapID = 1,
            IdType = idType
        };

        var results = Validate(dto);

        Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(ImageRequest.IdType)));
    }
    
    [Theory]
    [InlineData("CitizenID123")]
    [InlineData("InvalidID")]
    [InlineData("CitizenIDCitizenID")]
    public void IdType_AllowedTypes_FailsValidation(string idType)
    {
        var dto = new ImageRequest
        {
            Prompt = "A Cat",
            ImgID = "123",
            MapID = 1,
            IdType = idType
        };

        var results = Validate(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(ImageRequest.IdType)));
    }
    
    /*[Theory]
    [InlineData("VTA")]
    [InlineData("vta")]
    [InlineData("Vta")]
    [InlineData("FP")]
    [InlineData("fp")]
    [InlineData("Fp")]
    public void OriginApp_AllowedTypes_PassesValidation(string origin)
    {
        var dto = new ImageRequest
        {
            Prompt = "A Cat",
            ImgID = "123",
            MapID = 1,
            IdType = "CitizenID",
            OriginApp =  origin
        };

        var results = Validate(dto);

        Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(ImageRequest.IdType)));
    }*/
    
    [Theory]
    [InlineData(9)]
    [InlineData(10)]
    public void OriginApp_AtOrBelowMax_IsValid(int length)
    {
        var dto = new ImageRequest
        {
            Prompt = "A Cat", 
            ImgID = "123", 
            MapID = 1,  
            IdType = "CitizenID",
        };

        var results = Validate(dto);

        Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(ImageRequest.ImgID)));
    }

    [Theory]
    [InlineData(11)]
    [InlineData(100)]
    public void OriginApp_AboveMax_IsInvalid(int length)
    {
        var dto = new ImageRequest
        {
            Prompt = "A Cat",
            ImgID = new string('a', length),
            MapID = 1,
            IdType = "CitizenID"
        };

        var results = Validate(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(ImageRequest.ImgID)));
    }
    
    /*[Theory]
    [InlineData("vtafp")]
    [InlineData("fpfp")]
    [InlineData("vta ")]
    [InlineData("fp test")]
    public void OriginApp_RegularExpression_IsInvalid(string originApp)
    {
        var dto = new ImageRequest
        {
            Prompt = "A Cat", 
            ImgID = "123", 
            MapID = 1,  
            IdType = "CitizenID",
            OriginApp = originApp
        };

        var results = Validate(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(ImageRequest.ImgID)));
    }*/
}