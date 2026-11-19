// Builds the structured prompt sent to the language model for image descriptions.
namespace Giraf_AI.HelperClasses;

/// <summary>
/// Builds prompts for generating image descriptions.
/// </summary>

public class PromptBuilder
{
    public PromptBuilder(string str, bool placement, string path)
    {
        try
        {
            var fileContents = File.ReadAllText(path);
            Text = placement
                ? fileContents + ". " + str
                : str + ". " + fileContents;
        }
        catch (IOException e)
        {
            throw new IOException($"The file '{path}' could not be read.", e);
        }
    }

    public string Text { get; }
}