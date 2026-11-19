using Giraf_AI.Exceptions;
using Giraf_AI.HelperClasses;
using Giraf_AI.Test.TestHelpers;

namespace Giraf_AI.Test.HelperClasses;

public class PromptBuilderTest
{
    [Theory]
    [InlineData( true, "Master prompt. A Cat")]
    [InlineData(false, "A Cat. Master prompt")]
    public void PromptBuilder_AllPlacements_ProducesExpectedOutput(bool placement, string expected)
    {
        // Arrange
        var path = TempFileHelper.CreateTempFile("Master prompt");

        try
        {
            // Act 
            var result = new PromptBuilder("A Cat", placement, path);
            
            // Assert
            Assert.Equal(expected, result.Text);
        }
        finally
        {
            File.Delete(path);
        }
    }
    
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void PromptBuilder_InvalidMasterPrompt_ThrowsException(string masterPrompt)
    {
        // Arrange
        var path = TempFileHelper.CreateTempFile(masterPrompt);

        try
        {
            // Act & Assert
            Assert.Throws<InvalidMasterPromptException>(
                () => new PromptBuilder("A Cat", true, path));
        }
        finally
        {
            File.Delete(path);
        }
    }
    
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void PromptBuilder_InvalidPrompt_ThrowsException(string prompt)
    {
        // Arrange
        var path = TempFileHelper.CreateTempFile("masterPrompt");

        try
        {
            // Act & Assert
            Assert.Throws<InvalidPromptException>(
                () => new PromptBuilder(prompt, true, path));
        }
        finally
        {
            File.Delete(path);
        }
    }
}