using CarInsuranceBot.Application.Handlers;
using CarInsuranceBot.Application.Interfaces;
using CarInsuranceBot.Domain.Enums;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CarInsuranceBot.Application.StateMachine;

// Receives each Update from Telegram and decides which Handler to call
// based on the current state of the user's session.
public class BotDispatcher(
    ISessionService _sessionService,
    ITelegramBotClient _botClient,
    StartHandler _startHandler,
    DocumentHandler _documentHandler,
    ConfirmHandler _confirmHandler,
    PaymentHandler _paymentHandler,
    PolicyHandler _policyHandler)
{
    public async Task DispatchAsync(Update update, CancellationToken ct)
    {
        if (update.Message is not { } message)
            return;

        var chatId = message.Chat.Id;

        try
        {
            var session = _sessionService.GetOrCreate(chatId);

            if (message.Text == "/start")
            {
                _sessionService.Remove(chatId);
                session = _sessionService.GetOrCreate(chatId);
                await _startHandler.HandleAsync(session, message, ct);
                return;
            }

            await (session.State switch
            {
                BotState.WaitingForPassport =>
                    _documentHandler.HandlePassportAsync(session, message, ct),

                BotState.WaitingForVehicleDoc =>
                    _documentHandler.HandleVehicleDocAync(session, message, ct),

                BotState.ConfirmingPassportData =>
                    _confirmHandler.HandleConfirmAsync(session, message, isPassport: true, ct),

                BotState.ConfirmingVehicleData =>
                    _confirmHandler.HandleConfirmAsync(session, message, isPassport: false, ct),

                BotState.WaitingForPriceConfirmation =>
                    _paymentHandler.HandleAsync(session, message, ct),

                BotState.PolisyIssued =>
                    _policyHandler.HandleAsync(session, message, ct),

                _ => _startHandler.HandleAsync(session, message, ct),
            });
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Console.WriteLine($"BotDispatcher unhandled error: {ex.Message}");

            try
            {
                await _botClient.SendMessage(
                    chatId,
                    "Sorry, an unexpected error occurred. Please try again by typing /start.",
                    cancellationToken: ct);
            }
            catch
            {
                Console.WriteLine($"Failed to send error message to chatId: {chatId}");
            }
        }
    }
}