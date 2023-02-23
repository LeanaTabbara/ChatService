using System.Net;
using Azure;
using Azure.Storage.Blobs;
using Microsoft.Azure.Cosmos;
using ChatService.Dtos;
using ChatService.Storage.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ChatService.Storage;

public class CosmosProfileStore : IProfileStore
{
    private readonly CosmosClient _cosmosClient;
    private readonly BlobContainerClient _blobContainerClient;

    public CosmosProfileStore(CosmosClient cosmosClient, BlobContainerClient blobContainerClient)
    {
        _cosmosClient = cosmosClient;
        _blobContainerClient = blobContainerClient;
    }
    


    // DRY
    private Container Container => _cosmosClient.GetDatabase("profiles").GetContainer("profiles");
    


    public async Task UpsertProfile(Profile profile)
    {
        if (profile == null ||
            string.IsNullOrWhiteSpace(profile.username) ||
            string.IsNullOrWhiteSpace(profile.firstName) ||
            string.IsNullOrWhiteSpace(profile.lastName)
           )
        {
            throw new ArgumentException($"Invalid profile {profile}", nameof(profile));
        }

        await Container.UpsertItemAsync(ToEntity(profile));
    }

    public async Task<Profile?> GetProfile(string username)
    {
        try
        {
            var entity = await Container.ReadItemAsync<ProfileEntity>(
                id: username,
                partitionKey: new PartitionKey(username),
                new ItemRequestOptions
                {
                    ConsistencyLevel = ConsistencyLevel.Session
                }
            );
            return ToProfile(entity);
        }
        catch (CosmosException e)
        {
            if (e.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            throw;
        }
    }

    public async Task DeleteProfile(string username)
    {
        try
        {
            await Container.DeleteItemAsync<Profile>(
                id: username,
                partitionKey: new PartitionKey(username)
            );
        }
        catch (CosmosException e)
        {
            if (e.StatusCode == HttpStatusCode.NotFound)
            {
                return;
            }

            throw;
        }
    }

    private static ProfileEntity ToEntity(Profile profile)
    {
        return new ProfileEntity(
            partitionKey: profile.username,
            id: profile.username,
            profile.firstName,
            profile.lastName,
            profile.profilePictureId
        );
    }

    private static Profile ToProfile(ProfileEntity entity)
    {
        return new Profile(
            username: entity.id,
            entity.firstName,
            entity.lastName,
            entity.profilePictureId
        );
    }

    public async Task<UploadImageResponse> UploadImage(UploadImageRequest imageRequest)
    {
       
            var gui = Guid.NewGuid();
                await _blobContainerClient.UploadBlobAsync(gui.ToString(),
                    imageRequest.File.OpenReadStream());
            return new UploadImageResponse(gui.ToString());
    }

    public async Task<FileContentResult?> DownloadImage(string imageId)
    {
        if (imageId == null ||
            string.IsNullOrWhiteSpace(imageId) 
           )
        {
            throw new ArgumentException($"Invalid image id {imageId}");
        }

        var blobClient = _blobContainerClient.GetBlobClient(imageId);
        try
        {
            var response = await blobClient.DownloadAsync();
            await using var memoryStream = new MemoryStream();
            await response.Value.Content.CopyToAsync(memoryStream);
            var bytes = memoryStream.ToArray();

            // string downloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/Downloads/";
            // string filePath = Path.Combine(downloadsPath, blobClient.Name);
            //  File.WriteAllBytes(filePath, bytes);    

            return new FileContentResult(bytes, "image/jpeg");

        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
        
        
    }
}