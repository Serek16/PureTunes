using System.Net;

namespace EmySoundProject.Controllers;

using Microsoft.AspNetCore.Mvc;
using System.IO;

[Route("audio")]
public class AudioController : ControllerBase
{
    [HttpGet("{filePath}")]
    public IActionResult GetAudioFile(string filePath)
    {
        var actualFilePath = WebUtility.UrlDecode(filePath);

        if (!System.IO.File.Exists(actualFilePath))
        {
            return NotFound();
        }

        var fileStream = new FileStream(actualFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return File(fileStream, "audio/mpeg", true);  // assuming MP3 files, adjust MIME type if different
    }
}