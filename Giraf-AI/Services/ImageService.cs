using System.Text;
using System.Text.Json;
using Giraf_AI.DTOs;
using Giraf_AI.Models;
using Giraf_AI.Services.AI;

namespace Giraf_AI.Services;

public class ImageService
{
    private readonly TranslationService _translationService;
    private readonly ContentFilterService _contentFilterService;
    private readonly ImageGenService _imageGenerationService;
    private readonly TitleService _titleGeneratorService;
    
    public ImageService(
        TranslationService translationService,
        ContentFilterService contentFilterService,
        ImageGenService imageGenerationService,
        TitleService titleGeneratorService)
    {
        _translationService = translationService;
        _contentFilterService = contentFilterService;
        _imageGenerationService = imageGenerationService;
        _titleGeneratorService = titleGeneratorService;
    }
    
    public async Task<ImageRes> ProcessImageRequestAsync(ImageReq req)
    {
        var translation = await _translationService.GenerateTranslationAsync(req.Prompt);
        var contentFilter = await _contentFilterService.GenerateContentFilterAsync(translation.PromptEn);
        var titleEn = await _titleGeneratorService.GenerateTitleAsync(translation.PromptEn);
        return new ImageRes
        {
            ImageData = await _imageGenerationService.GenerateImageData(translation.PromptEn),
            ImageText = new ImageText
            {
                TitleDa = req.Prompt,
                TitleEn = translation.PromptEn
            }
        };
    }
}