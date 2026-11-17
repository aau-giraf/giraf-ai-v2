namespace Giraf_AI.HelperClasses;

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