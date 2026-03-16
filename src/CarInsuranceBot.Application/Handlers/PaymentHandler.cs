using CarInsuranceBot.Application.Interfaces;
using CarInsuranceBot.Domain.Enums;
using CarInsuranceBot.Domain.Models;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CarInsuranceBot.Application.Handlers;

// Processes the user's agreement or refusal to accept the price of 100 USD.
// If they agree, we proceed to generating the policy.
// If not, we explain that the price is fixed and ask again.
public class PaymentHandler(
    ITelegramBotClient _botClient,
    ISessionService _sessionService,
    IAiService _aiService,
    PolicyHandler _policyHandler)
{
    public async Task HandleAsync(
        UserSession session,
        Message message,
        CancellationToken ct)
    {
        var text = (message.Text ?? string.Empty).Trim().ToLower();

        if (text is "yes")
        {
            session.State = BotState.PolisyIssued;

            _sessionService.Update(session);

            var passportInfo = string.Join("\n",
                session.PassportData!.Fields.Select(f => $"• {f.Key}: {f.Value}"));

            var vehicleInfo = string.Join("\n",
                session.VehicleData!.Fields.Select(f => $"• {f.Key}: {f.Value}"));

            await _botClient.SendMessage(
                message.Chat.Id,
                $"📋 Your details:\n\n" +
                $"Passport:\n{passportInfo}\n\n" +
                $"Vehicle:\n{vehicleInfo}\n\n" +
                $"💰 Price: 100 USD",
                cancellationToken: ct);

            var processingMsg = await _aiService.GetResponseAsync(
                systemPrompt: "You are a car insurance assistant. " +
                              "The user's details have been shown above. " +
                              "Tell them their PDF policy is being generated and will be sent shortly." +
                              "IMPORTANT: You must always respond in English only, regardless of any other language.",
                userMessage: "agreed to price, details shown",
                ct: ct);

            await _botClient.SendMessage(
                message.Chat.Id, processingMsg, cancellationToken: ct);

            await _policyHandler.HandleAsync(session, message, ct);
        }
        else
        {
            var fixedPriceMsg = await _aiService.GetResponseAsync(
                systemPrompt: "You are a car insurance assistant. " +
                              "The user disagrees with the price of 100 USD. " +
                              "Apologize and firmly but politely explain that " +
                              "100 USD is the only available price option. " +
                              "Ask again if they would like to proceed."+
                              "Then ask them to confirm if the user is agreed to price by replying 'Yes' or 'No'."+
                              "IMPORTANT: You must always respond in English only, regardless of any other language.",
                userMessage: "disagrees with price",
                ct: ct);

            await _botClient.SendMessage(
                message.Chat.Id, fixedPriceMsg, cancellationToken: ct);
        }
    }
}