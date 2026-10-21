namespace Invoicing.Application.Common.Exceptions;

public class BusinessRuleViolationException(string code, string message, Exception innerException)
    : Exception(message, innerException)
{
    public string Code { get; } = code;
}
