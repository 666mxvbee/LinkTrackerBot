using Microsoft.AspNetCore.Mvc;
using LinkTracker.Shared.Models;
using LinkTracker.Scrapper.Repositories;

namespace LinkTracker.Scrapper.Controllers;

[ApiController]
[Route("links")]
public class LinksController(ILinkRepository repo) : ControllerBase
{
    [HttpGet]
    public IActionResult Get([FromHeader(Name = "Tg-Chat-Id")] long chatId, [FromQuery] string? tag = null)
    {
        if (!repo.ChatExists(chatId))
        {
            return NotFound("Chat is not registered");
        }
        
        var links = repo.GetLinks(chatId).ToList();

        if (!string.IsNullOrEmpty(tag))
        {
            links = links
                .Where(l => l.Tags != null && l.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                .ToList();
        }

        var responseLinks = links.Select(l => new LinkResponse(
            l.Id, 
            l.Url, 
            l.Tags ?? Array.Empty<string>()
        )).ToArray();

        return Ok(new ListLinksResponse(responseLinks, responseLinks.Length));
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