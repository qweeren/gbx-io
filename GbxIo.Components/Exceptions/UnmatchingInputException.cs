using System.Runtime.Serialization;

namespace GbxIo.Components.Exceptions;

public class UnmatchingInputException : Exception
{
    public UnmatchingInputException()
    {
    }

    public UnmatchingInputException(string? message) : base(message)
    {
    }

    public UnmatchingInputException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
