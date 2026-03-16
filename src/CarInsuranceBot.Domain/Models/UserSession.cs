using CarInsuranceBot.Domain.Enums;

namespace CarInsuranceBot.Domain.Models;

//The full state of one user in the current dialog.
public class UserSession
{
    public long ChatId { get; init; }
    public BotState State { get; set; } = BotState.Started;
    public ExtractedDocumentData? PassportData { get; set; }
    public ExtractedDocumentData? VehicleData { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}