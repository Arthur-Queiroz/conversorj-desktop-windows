namespace ConversorJ.Core;

public sealed class ConversionException : Exception
{
    public ConversionException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public ConversionException(string code, string message, Exception innerException)
        : base(message, innerException)
    {
        Code = code;
    }

    public string Code { get; }
}
