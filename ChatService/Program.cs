using Azure.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using ChatService.Configuration;
using ChatService.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Configuration
builder.Services.Configure<CosmosSettings>(builder.Configuration.GetSection("Cosmos"));
builder.Services.Configure<BlobSettings>(builder.Configuration.GetSection("BlobStorage"));

// Add Services
builder.Services.AddSingleton<IProfileStore, CosmosProfileStore>();
builder.Services.AddSingleton(sp =>
{
    var cosmosOptions = sp.GetRequiredService<IOptions<CosmosSettings>>();
    return new CosmosClient(cosmosOptions.Value.ConnectionString);
});
builder.Services.AddSingleton(sp =>
{
    var blobOptions = sp.GetRequiredService<IOptions<BlobSettings>>();
    return new  BlobContainerClient(blobOptions.Value.ConnectionString, "profilepictures");
});

// var blobServiceClient = new BlobServiceClient(
//     new Uri("https://chatserviceleanarim.blob.core.windows.net"),
//     new DefaultAzureCredential());
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }