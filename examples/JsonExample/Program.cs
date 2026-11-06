using HuntApi;

using var client = new HuntApiClient(); // reads HUNTAPI_API_KEY
var response = await client.MediaMetadataAsync(new MediaMetadataParams { Query = "https://www.youtube.com/watch?v=Z5YP8kznWZs" });
Console.WriteLine(response.Json);
