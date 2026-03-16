namespace CarInsuranceBot.Domain.Exceptions;

//Custom exception for document recognition errors.
public class DocumentParseException : Exception
{
    public DocumentParseException(string message)
        : base(message) { }
    public DocumentParseException(string message, Exception inner)
        : base(message, inner) { }
}