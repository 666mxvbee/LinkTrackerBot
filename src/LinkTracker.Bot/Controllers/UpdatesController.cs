using Microsoft.AspNetCore.Mvc;
using Telegram.Bot;
using LinkTracker.Shared.Models;

namespace LinkTracker.Bot.Controllers;

[ApiController]
[Route("updates")]
public class UpdatesController(
    ITelegramBotClient botClient, 
    ILogger<UpdatesController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> PostUpdate([FromBody] LinkUpdate update)
    {
        logger.LogInformation("Received update for URL: {Url}", update.Url);

        try
        {
            foreach (var chatId in update.TgChatIds)
            {
                var message = $"🔔 *Update found!*\n\n" +
                              $"Source: {update.Url}\n" +
                              $"Description: {update.Description}";

                await botClient.SendMessage(
                    chatId: chatId,
                    text: message,
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown
                );
            }

            return Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending update to Telegram");
            return StatusCode(500, "Internal Server Error");
        }
    }
}