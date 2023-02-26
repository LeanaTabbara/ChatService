using Microsoft.AspNetCore.Mvc;
using ChatService.Dtos;
using ChatService.Storage;

namespace ChatService.Controllers;

[ApiController]
[Route("[controller]")]
public class ChatController : ControllerBase
{
    private readonly IProfileStore _profileStore;

    public ChatController(IProfileStore profileStore)
    {
        _profileStore = profileStore;
    }
        
    [HttpGet("/profile/{username}")]
    public async Task<ActionResult<Profile>> GetProfile(string username)
    {
        var profile = await _profileStore.GetProfile(username);
        if (profile == null)
        {
            return NotFound($"A User with username {username} was not found");
        }
            
        return Ok(profile);
    }

    [HttpPost("/profile")]
    public async Task<ActionResult<Profile>> AddProfile(Profile profile)
    {
        var existingProfile = await _profileStore.GetProfile(profile.username);
        if (existingProfile != null)
        {
            return Conflict($"A user with username {profile.username} already exists");
        }

        await _profileStore.UpsertProfile(profile);
        return CreatedAtAction(nameof(GetProfile), new {username = profile.username},
            profile);
    }
    
    [HttpPost("/images")]
    public async Task<ActionResult<UploadImageResponse>>
        UploadImage([FromForm] UploadImageRequest request)
    {
        
        var response = await _profileStore.UploadImage(request);

        return Ok(response);


    }
    
    [HttpGet("/images/{id}")]
    public async Task<IActionResult>
        DownloadImage(string id)
    {
        
            var file=  await _profileStore.DownloadImage(id);
            if (file == null)
            {
                return NotFound($"An image with imageId {id} was not found");
            }
            return Ok(file);
        
         
    }
    

}