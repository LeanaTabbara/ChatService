
using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using ChatService.Dtos;
using ChatService.Storage;
using Microsoft.AspNetCore.Http;
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
    public async Task PostImage()
    {
        //Image image = Image.FromFile("monkey.jpeg", true);
        // var stream = new System.IO.MemoryStream();
        // image.Save(stream, ImageFormat.Jpeg);
        // stream.Position = 0;
        Stream imageStreamSource = new FileStream("monkey.jpeg", FileMode.Open, FileAccess.Read, FileShare.Read);
        HttpContent fileStreamContent = new StreamContent(imageStreamSource);
        fileStreamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
        {
            Name = "file",
            FileName = "image_test" // this is not important but must not be empty
        };
        
        using var formData = new MultipartFormDataContent();
        formData.Add(fileStreamContent);
        
        var response = await _httpClient.PostAsync("/images", formData);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // var fileInfo = new FileInfo($"monkey.jpeg");
        // var content = new MultipartFormDataContent();
        // var file = new FileStream(fileInfo.FullName, FileMode.Open);
        // var imageContent = new StreamContent(file);
        // imageContent.Headers.ContentType = new MediaTypeWithQualityHeaderValue("multipart/form-data");
        // content.Headers.ContentType = new MediaTypeWithQualityHeaderValue("multipart/form-data");
        // content.Add(imageContent, "image1", "monkey.jpeg");
        //
        // var response = await _httpClient.PostAsync($"/images", content);
        // Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
    
    
    [Fact]
    public async Task GetImage()
    {
        var id = "77aca9f6-325c-43e7-a9fe-0fc8553abef6";
        var response = await _httpClient.GetAsync($"/images/{id}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
    
    [Fact]
    public async Task GetImage_NotFound()
    {
        var id = "DC2B66AB-18C6-4A2D-8972-B01595F0BA9E";
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
    }
    
}