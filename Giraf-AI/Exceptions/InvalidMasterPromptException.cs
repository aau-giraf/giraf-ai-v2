namespace Giraf_AI.Exceptions;

public class InvalidMasterPromptException(string path) : Exception($"The master prompt file '{path}' is empty");