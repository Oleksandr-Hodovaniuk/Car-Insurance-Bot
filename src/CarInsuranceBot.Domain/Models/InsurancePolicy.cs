namespace CarInsuranceBot.Domain.Models;

// An insurance policy that is returned to the user.
public class InsurancePolicy
{
    public string PolicyNumber { get; init; } =
        Guid.NewGuid().ToString("N")[..10].ToUpper();
    public long ChatId { get; init; }
    public decimal Price { get; init; } = 100m;
    public string Currency { get; init; } = "USD";
    public string PolicyText { get; init; } = string.Empty;
    public byte[] PdfBytes { get; init; } = Array.Empty<byte>();
    public string FileName  => $"Policy_{PolicyNumber}.pdf";
    public DateTime IssuedAt { get; init; } = DateTime.UtcNow;
    public DateTime ValidUntill { get; init; } = DateTime.UtcNow.AddYears(1);
}