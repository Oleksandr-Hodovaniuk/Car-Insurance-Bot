using CarInsuranceBot.Application.Interfaces;
using CarInsuranceBot.Domain.Enums;
using CarInsuranceBot.Domain.Exceptions;
using CarInsuranceBot.Domain.Models;
using System.Data;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CarInsuranceBot.Application.Handlers;

// Handles receiving document photos from the user.
// Downloads photos from Telegram, transfers to Mindee, saves the result to the session.
public class DocumentHandler(
    ITelegramBotClient _botClient,
    ISessionService _sessionService,
    IAiService _aiService,
    IMindeeService _mindeeService)
    : BaseHandler(_botClient, _aiService)
{
    public async Task HandlePassportAsync(
        UserSession session,
        Message message,
        CancellationToken ct)
        => await HandleDocumentAsync(session, message, isPassport: true, ct);

    public async Task HandleVehicleDocAync(
        UserSession session,
        Message message,
        CancellationToken ct)
        => await HandleDocumentAsync(session, message, isPassport: false, ct);

    private async Task HandleDocumentAsync(
        UserSession session,
        Message message,
        bool isPassport,
        CancellationToken ct)
    {
        Console.WriteLine($"[DocumentHandler] isPassport={isPassport}, State={session.State}, HasPhoto={message.Photo is not null}");

        var docType = isPassport ? "passport" : "vehicle registration document";

        if (await TryHandleQuestionAsync(
           message,
           currentStepHint: $"waiting for user to send a photo of their {docType}",
           ct))
            return;

        if (message.Photo is null)
        {
            var reminder = await _aiService.GetResponseAsync(
                systemPrompt: "You are a car insurance assistant. " +
                              "The user sent text instead of a photo. " +
                             $"Politely remind them to send a photo of their {docType}."+
                            "IMPORTANT: You must always respond in English only, regardless of any other language.",
                userMessage: message.Text ?? string.Empty,
                ct: ct);

            await _botClient.SendMessage(
                message.Chat.Id,
                reminder,
                cancellationToken: ct);

            return;
        }

        var photo = message.Photo.Last();

        using var photoStream = new MemoryStream();

        await _botClient.GetInfoAndDownloadFile(photo.FileId, photoStream);

        photoStream.Position = 0;

        try
        {
            var extractedData = isPassport
                ? await _mindeeService.ExtractPassportDataAsync(photoStream, ct)
                : await _mindeeService.ExtractVehicleDocDataAsync(photoStream, ct);

            if (isPassport)
                session.PassportData = extractedData;
            else
                session.VehicleData = extractedData;

            var fieldsText = string.Join("\n",
                extractedData.Fields.Select(f => $"• {f.Key}: {f.Value}"));

            await _botClient.SendMessage(
                message.Chat.Id,
                $"Extracted data:\n\n{fieldsText}",
                cancellationToken: ct);

            var confirmationMsg = await _aiService.GetResponseAsync(
                systemPrompt: "You are a car insurance assistant. " +
                              "The extracted document data has already been displayed to the user above. " +
                              "Your task is ONLY to ask the user to confirm if the data is correct. " +
                              "Do NOT mention the data, do NOT say you cannot see it. " +
                              "Just ask: please reply 'Yes' if correct or 'No' if incorrect.",
                userMessage: "ask user to confirm",
                ct: ct);

            await _botClient.SendMessage(
                message.Chat.Id, confirmationMsg, cancellationToken: ct);

            session.State = isPassport
                ? BotState.ConfirmingPassportData
                : BotState.ConfirmingVehicleData;

            _sessionService.Update(session);
        }
        catch (DocumentParseException)
        {
            var errorMsg = await _aiService.GetResponseAsync(
                systemPrompt: "You are a car insurance assistant. " +
                              "The document photo could not be processed. " +
                              "Apologize and ask the user to retake the photo " +
                              "ensuring good lighting and that the document is fully visible."+
                              "IMPORTANT: You must always respond in English only, regardless of any other language.",
                userMessage: "document parse error",
                ct: ct);

            await _botClient.SendMessage(
                message.Chat.Id, errorMsg, cancellationToken: ct);
        }
    }
}