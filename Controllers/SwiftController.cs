using Microsoft.AspNetCore.Mvc;
using SwiftMT103Parser.Data;
using SwiftMT103Parser.Models;
using SwiftMT103Parser.Services;

namespace SwiftMT103Parser.Controllers;

[ApiController]
[Route("api/swift")]
public class SwiftController : ControllerBase
{
    private readonly Mt103ParserService _parser;
    private readonly DatabaseService _db;
    private readonly ILogger<SwiftController> _logger;

    public SwiftController(Mt103ParserService parser, DatabaseService db, ILogger<SwiftController> logger)
    {
        _parser = parser;
        _db = db;
        _logger = logger;
    }

    /// <summary>Upload a .txt file containing a raw MT103 message, parse it, and store to DB.</summary>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(Mt103Message), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file provided.");

        if (!file.FileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only .txt files are accepted.");

        string raw;
        using (var reader = new StreamReader(file.OpenReadStream()))
            raw = await reader.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(raw))
            return BadRequest("File is empty.");

        _logger.LogInformation("Received file '{FileName}' ({Size} bytes)", file.FileName, file.Length);

        Mt103Message parsed;
        try
        {
            parsed = _parser.Parse(raw);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse MT103 message");
            return BadRequest($"Parse error: {ex.Message}");
        }

        parsed.Id = _db.SaveMessage(parsed);
        return Ok(parsed);
    }

    /// <summary>Retrieve all stored MT103 messages.</summary>
    [HttpGet("messages")]
    [ProducesResponseType(typeof(List<Mt103Message>), StatusCodes.Status200OK)]
    public IActionResult GetMessages()
    {
        var messages = _db.GetAllMessages();
        _logger.LogInformation("Returning {Count} messages", messages.Count);
        return Ok(messages);
    }
}
