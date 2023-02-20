namespace ChatService.Storage.Entities;

public record ProfileEntity(string partitionKey, string id, string firstName, string lastName, string profilePictureId);