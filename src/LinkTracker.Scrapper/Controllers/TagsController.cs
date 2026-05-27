using LinkTracker.Scrapper.Repositories;
using LinkTracker.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace LinkTracker.Scrapper.Controllers;

[ApiController]
[Route("tags")]
public class TagsController(ITagRepository repo) : ControllerBase
{
    [HttpGet]
    public IActionResult GetAll([FromQuery] int offset = 0, [FromQuery] int limit = 100)
    {
        var tags = repo.GetAll(offset, limit).ToArray();

        return Ok(tags);
    }

    [HttpGet("{id:long}")]
    public IActionResult Get(long id)
    {
        var tag = repo.Get(id);

        return tag is null ? NotFound() : Ok(tag);
    }

    [HttpPost]
    public IActionResult Create([FromBody] CreateTagRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Tag name is required");
        }

        var tag = repo.Create(request.Name);

        return CreatedAtAction(nameof(Get), new { id = tag.Id }, tag);
    }

    [HttpPut("{id:long}")]
    public IActionResult Update(long id, [FromBody] UpdateTagRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Tag name is required");
        }

        var tag = repo.Update(id, request.Name);

        return tag is null ? NotFound() : Ok(tag);
    }

    [HttpDelete("{id:long}")]
    public IActionResult Delete(long id)
    {
        return repo.Delete(id) ? NoContent() : NotFound();
    }
}
