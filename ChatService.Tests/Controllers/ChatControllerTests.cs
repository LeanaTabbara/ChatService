
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
        var profile = new Profile("foobar", "Foo", "Bar", "AA24E24C-E576-4BA4-B8C1-A6B27474EE2C");
        var response = await _httpClient.PostAsync("/Profile",
            new StringContent(JsonConvert.SerializeObject(profile), Encoding.Default, "application/json"));
        
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("http://localhost/Profile/foobar", response.Headers.GetValues("Location").First());
        
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
    public async Task UpdateProfile()
    {
        var profile = new Profile("foobar", "Foo", "Bar", "AA24E24C-E576-4BA4-B8C1-A6B27474EE2C");
        _profileStoreMock.Setup(m => m.GetProfile(profile.username))
            .ReturnsAsync(profile);

        var updatedProfile = profile with { firstName = "Foo2" };

        var response = await _httpClient.PutAsync($"/Profile/{profile.username}",
            new StringContent(JsonConvert.SerializeObject(updatedProfile), Encoding.Default, "application/json"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _profileStoreMock.Verify(mock => mock.UpsertProfile(updatedProfile));
    }
    
    [Fact]
    public async Task UpdateProfile_NotFound()
    {
        // TODO: Complete
        
        var profile = new Profile("foobar", "Foo", "Bar","AA24E24C-E576-4BA4-B8C1-A6B27474EE2C");
        _profileStoreMock.Setup(m => m.GetProfile("foobar"))
            .ReturnsAsync((Profile?)null);
        
        var updatedProfile = profile with { firstName = "Foo2" };

        var response = await _httpClient.PutAsync($"/Profile/{profile.username}",
            new StringContent(JsonConvert.SerializeObject(updatedProfile), Encoding.Default, "application/json"));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        _profileStoreMock.Verify(mock => mock.UpsertProfile(updatedProfile), Times.Never);
    }
    
    [Theory]
  
    [InlineData( "", "Bar")]
    [InlineData( "   ", "Bar")]
    [InlineData( "Foo", "")]
    [InlineData( "Foo", null)]
    [InlineData( "Foo", " ")]
    public async Task UpdateProfile_InvalidArgs(string firstname, string lastname)
    {
        var profile = new Profile("foobar", "Foo", "Bar","AA24E24C-E576-4BA4-B8C1-A6B27474EE2C");

        var updatedProfile = profile with { firstName = firstname, lastName = lastname};

        var response = await _httpClient.PutAsync($"/Profile/{profile.username}",
            new StringContent(JsonConvert.SerializeObject(updatedProfile), Encoding.Default, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        _profileStoreMock.Verify(mock => mock.UpsertProfile(updatedProfile), Times.Never);
    }


    // [Fact]
    // public async Task PostImage()
    // {
    //     int width = 100;
    //     int height = 100;
    //     int channels = 3; // RGB
    //
    //     byte[] imageBytes = new byte[width * height * channels];
    //
    //     for (int y = 0; y < height; y++)
    //     {
    //         for (int x = 0; x < width; x++)
    //         {
    //             int offset = (y * width + x) * channels;
    //
    //             imageBytes[offset] = (byte)(255 * x / width);          // R channel
    //             imageBytes[offset + 1] = (byte)(255 * y / height);     // G channel
    //             imageBytes[offset + 2] = (byte)(255 * (x + y) / (width + height)); // B channel
    //         }
    //     }
    //
    //     MemoryStream stream = new MemoryStream(imageBytes);
    //     HttpContent fileStreamContent = new StreamContent(stream); // assuming you have your data in a stream
    //     fileStreamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
    //     {
    //         Name = "file",
    //         FileName = "anything" // this is not important but must not be empty
    //     };
    //
    //     using var formData = new MultipartFormDataContent();
    //     formData.Add(fileStreamContent);
    //     var response = await _httpClient.PostAsync("images", formData);
    //     Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    //
    //     //try1
    //     // string imagePath = "monkey.jpeg";
    //     // string absolutePath = Path.Combine(AppContext.BaseDirectory, imagePath);
    //     // using (FileStream fileStream = new FileStream(absolutePath, FileMode.Open, FileAccess.Read))
    //     // {
    //     //     MemoryStream memoryStream = new MemoryStream();
    //     //     await fileStream.CopyToAsync(memoryStream);
    //     //     memoryStream.Seek(0, SeekOrigin.Begin);
    //     //     FileStreamResult stream =  new FileStreamResult(memoryStream, "image/jpeg");
    //     //     
    //     //     HttpContent fileStreamContent = new StreamContent(stream.FileStream);
    //     //     fileStreamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
    //     //     {
    //     //         Name = "file",
    //     //         FileName = "image_test" // this is not important but must not be empty
    //     //     };
    //     //     
    //     //     using var formData = new MultipartFormDataContent();
    //     //     formData.Add(fileStreamContent);
    //     //     
    //     //     var response = await _httpClient.PostAsync("/images", formData);
    //     //     Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    //     //     
    //     // }
    //     
    //     
    //     //try2
    //     //Stream imageStreamSource = new FileStream("monkey.jpeg", FileMode.Open, FileAccess.Read, FileShare.Read);
    //     // string imagePath = "monkey.jpeg";
    //     // string absolutePath = Path.Combine(AppContext.BaseDirectory, imagePath);
    //     // //FileStream source = File.Open(absolutePath, FileMode.Open);
    //     // using (FileStream fileStream = new FileStream(absolutePath, FileMode.Open, FileAccess.Read))
    //     // {
    //     //     using (MemoryStream memoryStream = new MemoryStream())
    //     //     {
    //     //         await fileStream.CopyToAsync(memoryStream);
    //     //         memoryStream.Seek(0, SeekOrigin.Begin);
    //     //
    //     //         FileStreamResult source = new FileStreamResult(memoryStream, "image/jpeg");
    //     //         HttpContent fileStreamContent = new StreamContent(source.FileStream);
    //     //         Console.WriteLine(source.FileStream);
    //     //         fileStreamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
    //     //         {
    //     //             Name = "file",
    //     //             FileName = "image_test" // this is not important but must not be empty
    //     //         };
    //     //
    //     //         using var formData = new MultipartFormDataContent();
    //     //         formData.Add(fileStreamContent);
    //     //
    //     //         var response = await _httpClient.PostAsync("/images", formData);
    //     //         Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    //     //     }
    //     //
    //     // }
    //     
    //     //try3
    //     // var fileInfo = new FileInfo($"monkey.jpeg");
    //     // var content = new MultipartFormDataContent();
    //     // var file = new FileStream(fileInfo.FullName, FileMode.Open);
    //     // var imageContent = new StreamContent(file);
    //     // imageContent.Headers.ContentType = new MediaTypeWithQualityHeaderValue("multipart/form-data");
    //     // content.Headers.ContentType = new MediaTypeWithQualityHeaderValue("multipart/form-data");
    //     // content.Add(imageContent, "image1", "monkey.jpeg");
    //     //
    //     // var response = await _httpClient.PostAsync($"/images", content);
    //     // Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    //     
    //     
    //     //try4
    //     // int fileSize = 1024; // 1KB
    //     // byte[] fileContents = new byte[fileSize];
    //     //
    //     // // Generate random bytes for the file
    //     // Random rnd = new Random();
    //     // rnd.NextBytes(fileContents);
    //     //
    //     // // Create a memory stream and write the random bytes to it
    //     // using (var stream = new MemoryStream())
    //     // {
    //     //     stream.Write(fileContents, 0, fileSize);
    //     //     stream.Seek(0, SeekOrigin.Begin);
    //     //
    //     //     // Create a FileContentResult object and return it
    //     //     
    //     //     HttpContent fileStreamContent = new StreamContent(stream);
    //     //     fileStreamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
    //     //     {
    //     //         Name = "file",
    //     //         FileName = "image_test" // this is not important but must not be empty
    //     //     };
    //     //     
    //     //     using var formData = new MultipartFormDataContent();
    //     //     formData.Add(fileStreamContent);
    //     //     var response = await _httpClient.PostAsync("/images", formData);
    //     //     Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    //     // }
    // }
    //
    
    [Fact]
    public async Task GetImage()
    {
        var id = "77aca9f6-325c-43e7-a9fe-0fc8553abef6";
        var response = await _httpClient.GetAsync($"/images/{id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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
        var image = new Profile("foobar", "foo", "bar", id);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        //_profileStoreMock.Verify(mock => mock.UpsertProfile(image));
    }
    
}