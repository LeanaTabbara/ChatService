namespace ChatService.Configuration;

public record BlobSettings
{
    public string ConnectionString { get; init; }
}