
using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using ChatService.Dtos;
using ChatService.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace ChatService.Tests.Controllers;

public class ChatControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly Mock<IProfileStore> _profileStoreMock = new();
    private readonly HttpClient _httpClient;
    
    public ChatControllerTests(WebApplicationFactory<Program> factory)
    {
        // DRY: Don't repeat yourself
        _httpClient = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services => { services.AddSingleton(_profileStoreMock.Object); });
        }).CreateClient();
    }
    
    [Fact]
    public async Task GetProfile()
    {
        var profile = new Profile("rimbarakat", "rim", "barakat", "77aca9f6-325c-43e7-a9fe-0fc8553abef6");
        _profileStoreMock.Setup(m => m.GetProfile(profile.username))
            .ReturnsAsync(profile);
        
        var response = await _httpClient.GetAsync($"/profile/{profile.username}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Equal(profile, JsonConvert.DeserializeObject<Profile>(json));
    }
    
    [Fact]
    public async Task GetProfile_NotFound()
    {
        _profileStoreMock.Setup(m => m.GetProfile("rimbarakat"))
            .ReturnsAsync((Profile?)null);
    
        var response = await _httpClient.GetAsync($"/Profile/rimbarakat");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
    
    [Fact]
    public async Task AddProfile()
    {
        var profile = new Profile("foobar", "Foo", "Bar",  Guid.NewGuid().ToString());
        var response = await _httpClient.PostAsync("/profile",
            new StringContent(JsonConvert.SerializeObject(profile), Encoding.Default, "application/json"));
        
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("http://localhost/profile/foobar", response.Headers.GetValues("Location").First());
        
        _profileStoreMock.Verify(mock => mock.UpsertProfile(profile), Times.Once);
    }

    [Fact]
    public async Task AddProfile_Conflict()
    {
        var profile = new Profile("foobar", "Foo", "Bar","AA24E24C-E576-4BA4-B8C1-A6B27474EE2C");
        _profileStoreMock.Setup(m => m.GetProfile(profile.username))
            .ReturnsAsync(profile);

        var response = await _httpClient.PostAsync("/Profile",
            new StringContent(JsonConvert.SerializeObject(profile), Encoding.Default, "application/json"));
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        
        _profileStoreMock.Verify(m => m.UpsertProfile(profile), Times.Never);
    }
    
    [Theory]
    [InlineData(null, "Foo", "Bar","AA24E24C-E576-4BA4-B8C1-A6B27474EE2C")]
    [InlineData("", "Foo", "Bar","AA24E24C-E576-4BA4-B8C1-A6B27474EE2C")]
    [InlineData(" ", "Foo", "Bar","AA24E24C-E576-4BA4-B8C1-A6B27474EE2C")]
    [InlineData("foobar", null, "Bar","AA24E24C-E576-4BA4-B8C1-A6B27474EE2C")]
    [InlineData("foobar", "", "Bar","AA24E24C-E576-4BA4-B8C1-A6B27474EE2C")]
    [InlineData("foobar", "   ", "Bar","AA24E24C-E576-4BA4-B8C1-A6B27474EE2C")]
    [InlineData("foobar", "Foo", "","AA24E24C-E576-4BA4-B8C1-A6B27474EE2C")]
    [InlineData("foobar", "Foo", null,"AA24E24C-E576-4BA4-B8C1-A6B27474EE2C")]
    [InlineData("foobar", "Foo", " ","AA24E24C-E576-4BA4-B8C1-A6B27474EE2C")]
    [InlineData("foobar", "Foo", "Bar",null)]
    [InlineData("foobar", "Foo", "Bar","")]
    [InlineData("foobar", "Foo", "Bar"," ")]
    public async Task AddProfile_InvalidArgs(string username, string firstName, string lastName, string profilePictureId)
    {
        var profile = new Profile(username, firstName, lastName, profilePictureId);
        var response = await _httpClient.PostAsync("/Profile",
            new StringContent(JsonConvert.SerializeObject(profile), Encoding.Default, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        _profileStoreMock.Verify(mock => mock.UpsertProfile(profile), Times.Never);
    }
    
    
    [Fact]
    public async Task GetImage()
    {
        int fileSize = 1024; // 1KB
        byte[] fileContents = new byte[fileSize];

        // Generate random bytes for the file
        Random rnd = new Random();
        rnd.NextBytes(fileContents);
        
        String fileName = Guid.NewGuid().ToString();

        // Create a memory stream and write the random bytes to it
        var stream = new MemoryStream();
        
            stream.Write(fileContents, 0, fileSize);
            stream.Seek(0, SeekOrigin.Begin);

            // Create a FileContentResult object and return it
             FileContentResult file = new FileContentResult(stream.ToArray(), "application/octet-stream")
            {
                FileDownloadName = fileName
            };

             _profileStoreMock.Setup(m => m.DownloadImage(fileName))
            .ReturnsAsync(file);
        var response = await _httpClient.GetAsync($"/images/{fileName}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
    
    [Fact]
    public async Task GetImage_NotFound()
    {
        String fileName = Guid.NewGuid().ToString();
        _profileStoreMock.Setup(m => m.DownloadImage(fileName))
            .ReturnsAsync((FileContentResult) null);
        var id = Guid.NewGuid().ToString();
        var response = await _httpClient.GetAsync($"/images/{id}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task GetImage_InvalidArgs(string id)
    {
        var response = await _httpClient.GetAsync($"/images/{id}");
        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        _profileStoreMock.Verify(mock => mock.DownloadImage(id), Times.Never);
    }

    [Fact]
    public async Task UploadImage()
    {
        int length = 1024;
        String fileName = Guid.NewGuid().ToString();
        var content = new byte[length];
        var random = new Random();
        random.NextBytes(content);

        var stream = new MemoryStream(content);
        UploadImageResponse response = new UploadImageResponse("anything");
        _profileStoreMock.Setup(m => m.UploadImage(It.IsAny<UploadImageRequest>()))
            .ReturnsAsync(response);
        
        HttpContent fileStreamContent = new StreamContent(stream); // assuming you have your data in a stream
        fileStreamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
        {
            Name = "file",
            FileName = "anything" ,// this is not important but must not be empty
        };
        fileStreamContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

        using var formData = new MultipartFormDataContent();
        formData.Add(fileStreamContent);
        
        var httpResponse = await _httpClient.PostAsync($"/images", formData);
        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        var json = await httpResponse.Content.ReadAsStringAsync();
        Assert.Equal(response, JsonConvert.DeserializeObject<UploadImageResponse>(json));
        
    }

    [Fact]
    public async Task UploadNullRequest()
    {
        using var formData = new MultipartFormDataContent();
        
        var httpResponse = await _httpClient.PostAsync($"/images", formData);
        Assert.Equal(HttpStatusCode.BadRequest, httpResponse.StatusCode);
        _profileStoreMock.Verify(mock => mock.UploadImage(It.IsAny<UploadImageRequest>()), Times.Never);
    }
}