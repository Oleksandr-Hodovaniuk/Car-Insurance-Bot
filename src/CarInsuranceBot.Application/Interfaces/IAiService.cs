namespace CarInsuranceBot.Application.Interfaces;

//Contract for working with OpenAI.
public interface IAiService
{
    //A general method for AI responses in dialogue.
    Task<string> GetResponseAsync(
        string systemPrompt,
        string userMessage,
        CancellationToken ct = default);

    //A method for generating policy text.
    Task<string> GeneratePolicyTextAsync(
        string passportInfo,
        string vehicleInfo,
        CancellationToken ct = default);
}