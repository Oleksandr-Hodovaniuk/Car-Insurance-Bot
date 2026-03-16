using CarInsuranceBot.Application.Interfaces;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;

namespace CarInsuranceBot.Infrastructure.Services;

public class GroqService(ChatClient _chatClient, ILogger<GroqService> _logger) : IAiService
{
    public async Task<string> GetResponseAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken ct = default)
    {
        try
        {
            var message = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userPrompt)
            };

            var response = await _chatClient.CompleteChatAsync(
                message,
                cancellationToken: ct);

            return response.Value.Content[0].Text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Groq GetResponseAsync failed");

            return "I'm sorry, I'm having trouble processing your request. Please try again.";
        }
    }

    public async Task<string> GeneratePolicyTextAsync(
        string passportInfo,
        string vehicleInfo,
        CancellationToken ct = default)
    {
        var systemPrompt =
             "You are an insurance document generator. " +
             "Generate a formal car insurance policy document based on the provided data. " +
             "Include: policyholder name, vehicle details, coverage period (1 year), " +
             "coverage amount, and standard terms and conditions. " +
             "Format it as a professional insurance document with clear sections." +
             "IMPORTANT: You must always respond in English only, regardless of any other language.";

        var userMessage =
           $"Generate an insurance policy for:\n" +
           $"Passport data: {passportInfo}\n" +
           $"Vehicle data: {vehicleInfo}";

        return await GetResponseAsync(systemPrompt, userMessage, ct);
    }
}