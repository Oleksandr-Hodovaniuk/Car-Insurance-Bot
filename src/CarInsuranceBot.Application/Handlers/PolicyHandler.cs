using CarInsuranceBot.Application.Interfaces;
using CarInsuranceBot.Domain.Models;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CarInsuranceBot.Application.Handlers;

// The final step — generates a PDF policy and sends it to the user as a document.
// Does not use IAiService directly — AI is called inside PolicyService.
public class PolicyHandler(
    ITelegramBotClient _botClient,
    ISessionService _sessionService,
    IPolicyService _policyService)
{
    public async Task HandleAsync(
        UserSession session,
        Message message,
        CancellationToken ct) 
    {
        var policy = await _policyService.GenerateAsync(session, ct);

        using var pdfStream = new MemoryStream(policy.PdfBytes);

        var inputFile = new InputFileStream(pdfStream, policy.FileName);

        await _botClient.SendDocument(
            chatId: message.Chat.Id,
            document: inputFile,
            caption: $"Your insurance policy #{policy.PolicyNumber}\n" +
                     $"Valid until: {policy.ValidUntill:dd MMMM yyyy}\n" +
                     $"Price: {policy.Price} {policy.Currency}",
            cancellationToken: ct);

        _sessionService.Remove(session.ChatId);
    }
}