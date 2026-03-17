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

        if (await TryHandleQuestionAsync(
            message,
            currentStepHint: $"waiting for user to confirm if {docType} data is correct by replying Yes or No",
            ct))
            return;
        var text = (message.Text ?? string.Empty).Trim().ToLower();

        if (text is "yes")
        {
            if (isPassport)
                session.PassportData!.IsConfirmed = true;
            else
                session.VehicleData!.IsConfirmed = true;

            string nextMessage;

            if (isPassport)
            {
                session.State = BotState.WaitingForVehicleDoc;

                nextMessage = await _aiService.GetResponseAsync(
                    systemPrompt: "You are a car insurance assistant. " +
                                  "The passport data was confirmed. " +
                                  "Now ask the user to send a photo of their " +
                                  "vehicle registration document."+
                                  "IMPORTANT: You must always respond in English only, regardless of any other language.",
                    userMessage: "passport confirmed",
                    ct: ct);
            }
            else
            {
                session.State = BotState.WaitingForPriceConfirmation;

                nextMessage = await _aiService.GetResponseAsync(
                    systemPrompt: "You are a car insurance assistant. " +
                                  "Both documents are confirmed. " +
                                  "Inform the user that the insurance price is 100 USD. " +
                                  "Ask if they agree to proceed with the purchase. Reply Yes or No." +
                                  "IMPORTANT: You must always respond in English only, regardless of any other language.",
                    userMessage: "vehicle doc confirmed",
                    ct: ct);
            }

            await _botClient.SendMessage(
               message.Chat.Id, nextMessage, cancellationToken: ct);
        }
        else if (text is "no" or "ні" or "n")
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
        }
        else
        {
            await _botClient.SendMessage(message.Chat.Id,
                "Please reply 'Yes' if the data is correct or 'No' if you'd like to retake the photo.",
                cancellationToken: ct);
            return;
        }
        _sessionService.Update(session);
    }
}