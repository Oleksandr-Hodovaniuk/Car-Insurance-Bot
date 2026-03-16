namespace CarInsuranceBot.Domain.Models;

//The result of recognizing one document via Mindee API.
public class ExtractedDocumentData
{
    public string RawText { get; set; } = string.Empty;
    public Dictionary<string, string> Fields { get; init; } = new();
    public bool IsConfirmed { get; set; }
}