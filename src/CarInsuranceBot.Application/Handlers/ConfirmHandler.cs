using CarInsuranceBot.Application.Interfaces;
using CarInsuranceBot.Domain.Enums;
using CarInsuranceBot.Domain.Models;
using System.Data;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CarInsuranceBot.Application.Handlers;

// Processes the user's response to confirm the recognized document data.
// "Yes" — proceed to the next step, "No" — please take another photo.
public class ConfirmHandler(
    ITelegramBotClient _botClient,
    ISessionService _sessionService,
    IAiService _aiService)
    : BaseHandler(_botClient, _aiService)
{
    public async Task HandleConfirmAsync(
    UserSession session,
    Message message,
    bool isPassport,
    CancellationToken ct)
    {
        var docType = isPassport ? "passport" : "vehicle document";
        var text = (message.Text ?? string.Empty).Trim().ToLower();

        if (text is "yes")
        {
            if (isPassport)
                session.PassportData!.IsConfirmed = true;
            else
                session.VehicleData!.IsConfirmed = true;

            if (isPassport)
            {
                session.State = BotState.WaitingForVehicleDoc;

                var nextMsg = await _aiService.GetResponseAsync(
                    systemPrompt: "You are a car insurance assistant. " +
                                  "The passport data was confirmed by the user. " +
                                  "Congratulate them briefly and ask them to send a photo of their vehicle registration document. " +
                                  "RESPOND IN ENGLISH ONLY.",
                    userMessage: "passport confirmed",
                    ct: ct);

                await _botClient.SendMessage(message.Chat.Id, nextMsg, cancellationToken: ct);
            }
            else
            {
                session.State = BotState.WaitingForPriceConfirmation;

                var nextMsg = await _aiService.GetResponseAsync(
                    systemPrompt: "You are a car insurance assistant. " +
                                  "The vehicle document data was confirmed by the user. " +
                                  "Inform them the insurance price is 100 USD and ask if they agree to proceed. " +
                                  "Ask them to reply 'Yes' or 'No'. " +
                                  "RESPOND IN ENGLISH ONLY.",
                    userMessage: "vehicle doc confirmed",
                    ct: ct);

                await _botClient.SendMessage(message.Chat.Id, nextMsg, cancellationToken: ct);
            }

            _sessionService.Update(session);
            return;
        }

        if (text is "no" or "ні" or "n")
        {
            session.State = isPassport
                ? BotState.WaitingForPassport
                : BotState.WaitingForVehicleDoc;

            var retryMsg = await _aiService.GetResponseAsync(
                systemPrompt: "You are a car insurance assistant. " +
                             $"User rejected {docType} data. Ask them to retake the photo. " +
                              "RESPOND IN ENGLISH ONLY.",
                userMessage: "data rejected",
                ct: ct);

            await _botClient.SendMessage(message.Chat.Id, retryMsg, cancellationToken: ct);
            _sessionService.Update(session);
            return;
        }

        if (await TryHandleQuestionAsync(
            message,
            currentStepHint: $"waiting for user to confirm if {docType} data is correct by replying Yes or No",
            ct))
            return;

        await _botClient.SendMessage(message.Chat.Id,
            "Please reply 'Yes' if the data is correct or 'No' if you'd like to retake the photo.",
            cancellationToken: ct);
    }
}