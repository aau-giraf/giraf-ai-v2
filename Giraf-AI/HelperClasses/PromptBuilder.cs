using Giraf_AI.Exceptions;

// Builds the structured prompt sent to the language model for image descriptions.
namespace Giraf_AI.HelperClasses;

/// <summary>
/// Builds prompts for generating image descriptions.
/// </summary>
public class PromptBuilder
{
    /// If placement is true, the master prompt comes before the prompt/str
    /// Certain models return greater result in specific order
    public PromptBuilder(string str, bool placement, string path)
    {
        try
        {
            // Read the contents of the specified file and construct the prompt text based on the placement flag
            var fileContents = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(fileContents))
            {
                throw new InvalidMasterPromptException(path);
            }

            if (string.IsNullOrWhiteSpace(str))
            {
                throw new InvalidPromptException(str);
            }
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