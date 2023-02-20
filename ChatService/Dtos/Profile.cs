using System.ComponentModel.DataAnnotations;

namespace ChatService.Dtos;

public record Profile(
    [Required] string username, 
    [Required] string firstName, 
    [Required] string lastName,
     string profilePictureId);
    
public record UploadImageRequest(IFormFile File);

public record UploadImageResponse([Required] string imageId);