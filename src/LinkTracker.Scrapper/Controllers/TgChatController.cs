using Microsoft.AspNetCore.Mvc;
using LinkTracker.Shared.Models;
using LinkTracker.Scrapper.Repositories;

namespace LinkTracker.Scrapper.Controllers;

[ApiController]
[Route("tg-chat")]
public class TgChatController(ILinkRepository repo) : ControllerBase
{
    [HttpPost("{id}")]
    public IActionResult Register(long id) { repo.AddChat(id); return Ok(); }

    [HttpDelete("{id}")]
    public IActionResult Delete(long id)
    {
        if (!repo.ChatExists(id)) { return NotFound(); }
        repo.RemoveChat(id);
        return Ok();
    }
}