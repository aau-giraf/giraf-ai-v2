namespace Giraf_AI.Exceptions;

public class InvalidPromptException(string prompt) : Exception($"The prompt: '{prompt}' is empty");