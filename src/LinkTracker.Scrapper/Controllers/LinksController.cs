using Microsoft.AspNetCore.Mvc;
using LinkTracker.Shared.Models;
using LinkTracker.Scrapper.Repositories;

namespace LinkTracker.Scrapper.Controllers;

[ApiController]
[Route("links")]
public class LinksController(ILinkRepository repo) : ControllerBase
{
    [HttpGet]
    public IActionResult Get([FromHeader(Name = "Tg-Chat-Id")] long chatId)
    {
        if (!repo.ChatExists(chatId))
        {
            return NotFound("Chat is not registered");
        }

        return Ok(repo.GetLinks(chatId));
    }

    [HttpPost]
    public IActionResult Add([FromHeader(Name = "Tg-Chat-Id")] long chatId, [FromBody] AddLinkRequest req)
    {
        if (!repo.ChatExists(chatId))
        {
            return NotFound("Chat is not registered");
        }

        var link = repo.AddLink(chatId, req.Link, req.Tags);
        if (link == null)
        {
            return Conflict(new { description = "Links is already registered" });
        }

        return Ok(link);
    }

    [HttpDelete]
    public IActionResult Remove([FromHeader(Name = "Tg-Chat-Id")] long chatId, [FromBody] RemoveLinkRequest req)
    {
        if (!repo.ChatExists(chatId))
        {
            return NotFound("Chat is not registered");
        }

        if (!repo.RemoveLink(chatId, req.Link))
        {
            return NotFound("Link is not registered");
        }
        
        return Ok(req);
    }
}