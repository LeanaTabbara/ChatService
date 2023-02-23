using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ChatService.Dtos;
using ChatService.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ChatService.IntegrationTests;

public class CosmosProfileStoreTest : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly IProfileStore _store;

    private readonly Profile _profile = new(
        username: Guid.NewGuid().ToString(),
        firstName: "Foo",
        lastName: "Bar",
        profilePictureId: Guid.NewGuid().ToString()
    );
    
    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _store.DeleteProfile(_profile.username);
    }

    public CosmosProfileStoreTest(WebApplicationFactory<Program> factory)
    {
        _store = factory.Services.GetRequiredService<IProfileStore>();
    }
    
    
    [Fact]
    public async Task AddNewProfile()
    {
        await _store.UpsertProfile(_profile);
        Assert.Equal(_profile, await _store.GetProfile(_profile.username));
    }

    [Fact]
    public async Task GetNonExistingProfile()
    {
        Assert.Null(await _store.GetProfile(_profile.username));
    }
    
    [Fact]
    public async Task UpdateExistingProfile()
    {
        var profile = new Profile(username: "foobar", firstName: "Foo", lastName: "Bar", profilePictureId:Guid.NewGuid().ToString());
        await _store.UpsertProfile(profile);

        var updatedProfile = profile with { firstName = "Foo1", lastName = "Foo2" };
        await _store.UpsertProfile(updatedProfile);
        
        Assert.Equal(updatedProfile, await _store.GetProfile(profile.username));
    }
    
    [Theory]
    [InlineData(null, "Foo", "Bar", "4372D709-4CFA-4EF3-9FF8-7359E56F83CD")]
    [InlineData("", "Foo", "Bar","4372D709-4CFA-4EF3-9FF8-7359E56F83CD")]
    [InlineData(" ", "Foo", "Bar","4372D709-4CFA-4EF3-9FF8-7359E56F83CD")]
    [InlineData("foobar", null, "Bar","4372D709-4CFA-4EF3-9FF8-7359E56F83CD")]
    [InlineData("foobar", "", "Bar","4372D709-4CFA-4EF3-9FF8-7359E56F83CD")]
    [InlineData("foobar", "   ", "Bar","4372D709-4CFA-4EF3-9FF8-7359E56F83CD")]
    [InlineData("foobar", "Foo", "","4372D709-4CFA-4EF3-9FF8-7359E56F83CD")]
    [InlineData("foobar", "Foo", null,"4372D709-4CFA-4EF3-9FF8-7359E56F83CD")]
    [InlineData("foobar", "Foo", " ","4372D709-4CFA-4EF3-9FF8-7359E56F83CD")]
    [InlineData("foobar", "Foo", " ",null)]
    [InlineData("foobar", "Foo", " ","")]
    [InlineData("foobar", "Foo", " "," ")]
    public async Task UpsertProfile_InvalidArgs(string username, string firstName, string lastName, string profilePictureId)
    {
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await _store.UpsertProfile(new Profile(username, firstName, lastName, profilePictureId));
        });
    }
    
    
    [Fact]
    public async Task DeleteProfile()
    {
        var profile = new Profile(username: "foobar", firstName: "Foo", lastName: "Bar", profilePictureId:Guid.NewGuid().ToString());
        await _store.UpsertProfile(profile);

        Assert.Equal(profile, await _store.GetProfile(profile.username));

        await _store.DeleteProfile(profile.username);
        
        Assert.Null(await _store.GetProfile(profile.username));
        
    }
    
    [Fact]
    public async Task DeleteNonExistingProfile()
    {
        var profile = new Profile(username: "foobar", firstName: "Foo", lastName: "Bar", profilePictureId:Guid.NewGuid().ToString());
      
        await _store.DeleteProfile(profile.username);
        
        Assert.Null(await _store.GetProfile(profile.username));
        
    }
    
    
    
    //// NEW CONTROLLER TESTS FOR IMAGE DOWNLOAD AND UPLOAD ////
    [Fact]
    public async Task GetImage()
    {
        
        var id = "77aca9f6-325c-43e7-a9fe-0fc8553abef6"; //should upload an image then downlaod it 
        Assert.NotNull(await _store.DownloadImage(id));
    }
    
    [Fact]
    public async Task GetImage_NotFound()
    {
        var id = Guid.NewGuid().ToString();
        Assert.Null(await _store.DownloadImage(id));
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task GetImage_InvalidArgs(string id)
    {
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await _store.DownloadImage(id);
        });
        
    }
    
   //  [Fact]
   // // public async Task UploadImage()
   //  {
   //      int width = 100;
   //      int height = 100;
   //      int channels = 3; // RGB
   //
   //      byte[] imageBytes = new byte[width * height * channels];
   //
   //      for (int y = 0; y < height; y++)
   //      {
   //          for (int x = 0; x < width; x++)
   //          {
   //              int offset = (y * width + x) * channels;
   //
   //              imageBytes[offset] = (byte)(255 * x / width);          // R channel
   //              imageBytes[offset + 1] = (byte)(255 * y / height);     // G channel
   //              imageBytes[offset + 2] = (byte)(255 * (x + y) / (width + height)); // B channel
   //          }
   //      }
   //
   //      MemoryStream stream = new MemoryStream(imageBytes);
   //      HttpContent fileStreamContent = new StreamContent(stream); 
   //      fileStreamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
   //      {
   //          Name = "file",
   //          FileName = "anything" // this is not important but must not be empty
   //      };
   //      using var formData = new MultipartFormDataContent();
   //      formData.Add(fileStreamContent);
   //      string fileContents = await fileStreamContent.ReadAsStringAsync();
   //      IFormFile file = new FormFile(stream, 0, imageBytes.Length, 
   //          fileStreamContent.Headers.ContentDisposition.FileName.Trim('"'), 
   //          fileStreamContent.Headers.ContentType.MediaType);
   //      UploadImageRequest res = new UploadImageRequest(file);
   //      Assert.NotNull(await _store.UploadImage(res));
   //
   //  }


   [Fact]
   public async Task UploadImage()
   {
       int length = 1024;
       //String fileName = Guid.NewGuid().ToString();
       var content = new byte[length];
       var random = new Random();
       random.NextBytes(content);

       var stream = new MemoryStream(content);
       IFormFile file = new FormFile(stream, 0, length, "file", "anything"){
           Headers = new HeaderDictionary(),
           ContentType = "image/jpeg"
       };
       file.Headers["Content-Disposition"] = new ContentDispositionHeaderValue("form-data")
       {
           Name = "file",
           FileName = "anything",// this is not important but must not be empty
       }.ToString();
       UploadImageRequest request = new UploadImageRequest(file);
       
       var requestResult = await _store.UploadImage(request);
       var result = await _store.DownloadImage(requestResult.imageId);
       Assert.Equal(request.File.Length, result.FileContents.Length);
       Assert.Equal(request.File.ContentType , result.ContentType);
   }
}