using ChatService.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace ChatService.Storage;

public interface IProfileStore
{
    Task UpsertProfile(Profile profile);
    Task<Profile?> GetProfile(string username);
    Task DeleteProfile(string username);
    Task<UploadImageResponse> UploadImage(UploadImageRequest imageRequest);
    Task<FileContentResult?>  DownloadImage(String imageId);
}