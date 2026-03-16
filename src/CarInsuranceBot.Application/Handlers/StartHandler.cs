using CarInsuranceBot.Application.Interfaces;
using CarInsuranceBot.Domain.Enums;
using CarInsuranceBot.Domain.Models;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CarInsuranceBot.Application.Handlers;

// Processes the /start command — the first step of the bot.
// Greets the user via Groq and asks them to send a passport photo.
public class StartHandler(
    ITelegramBotClient _botClient,
    ISessionService _sessionService,
    IAiService _aiService)
{
    public async Task HandleAsync(
        UserSession session,
        Message message,
        CancellationToken ct = default)
    {
        var response = await _aiService.GetResponseAsync(
            systemPrompt: "You are a friendly car insurance assistant. " +
                          "Greet the user and explain that you help purchase car insurance. " +
                          "Ask them to send a photo of their passport to begin. " +
                          "Be concise and professional."+
                          "IMPORTANT: You must always respond in English only, regardless of any other language.",
            userMessage: "start",
            ct: ct);

        await _botClient.SendMessage(
            chatId: message.Chat.Id,
            text: response,
            cancellationToken: ct);

        session.State = BotState.WaitingForPassport;

        _sessionService.Update(session);
    }
}