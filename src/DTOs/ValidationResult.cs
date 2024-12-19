namespace AzureBlobStorageForeach.DTOs;

public class TemplateValidationResult
{
    public List<ValidationError> errors { get; set; } = new();
    public bool isValid { get; set; }

    public override string ToString()
    {
        if (isValid)
            return "Validation succeeded: No errors.";

        var errorsString = string.Join(Environment.NewLine, errors.Select(e => e.ToString()));
        return $"Validation failed with {errors.Count} error(s):{Environment.NewLine}{errorsString}";
    }
}

public class ValidationError
{
    public string errorCode { get; set; } = string.Empty;
    public string message { get; set; } = string.Empty;
    public string pointer { get; set; } = string.Empty;

    public override string ToString()
    {
        return $"   ErrorCode: {errorCode}, Message: {message}, Pointer: {pointer}";
    }
}
