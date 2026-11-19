using Giraf_AI.Exceptions;

namespace Giraf_AI.HelperClasses;

public class PromptBuilder
{
    /// If placement is true, the master prompt comes before the prompt/str
    /// Certain models return greater result in specific order
    public PromptBuilder(string str, bool placement, string path)
    {
        try
        {
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