using Microsoft.AspNetCore.Mvc;
using LinkTracker.Bot.Services.Notifications;
using LinkTracker.Shared.Models;

namespace LinkTracker.Bot.Controllers;

[ApiController]
[Route("updates")]
public class UpdatesController(
    ILinkUpdateHandler linkUpdateHandler,
    ILogger<UpdatesController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> PostUpdate(
        [FromBody] LinkUpdate update,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Received update for URL: {Url}", update.Url);

        try
        {
            await linkUpdateHandler.HandleAsync(update, cancellationToken);
            return Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending update to Telegram");
            return StatusCode(500, "Internal Server Error");
        }
    }
}
