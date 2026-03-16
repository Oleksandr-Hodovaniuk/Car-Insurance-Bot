using CarInsuranceBot.Application.StateMachine;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;

[ApiController]
[Route("webhook")]
public class BotController : ControllerBase
{
    private readonly BotDispatcher _botDispatcher;
    private readonly string _secretToken;

    public BotController(BotDispatcher botDispatcher)
    {
        _botDispatcher = botDispatcher;

        _secretToken = Environment.GetEnvironmentVariable("BotSettings__WebhookSecretToken")!;
    }
    [HttpPost]
    public async Task<IActionResult> Post(
        [FromBody] Update update,
        CancellationToken ct)
    {

        var token = Request.Headers["X-Telegram-Bot-Api-Secret-Token"]
             .FirstOrDefault();

        if (token != _secretToken)
            return Unauthorized();

        await _botDispatcher.DispatchAsync(update, ct);

        return Ok();
    }
}