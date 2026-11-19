namespace Giraf_AI.Test.TestHelpers;

public static class TempFileHelper
{
    public static string CreateTempFile(string content)
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, content);
        return path;
    }
}