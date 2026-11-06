using HuntApi;

using var client = new HuntApiClient(); // reads HUNTAPI_API_KEY
var submitted = await client.VideoDownloadAsync(new VideoDownloadParams { Query = "https://www.youtube.com/watch?v=Z5YP8kznWZs", VideoQuality = "720p" });
var job = await client.WaitForJobAsync(
    submitted.Json.GetProperty("job_id").GetString()!,
    onProgress: j => Console.WriteLine($"{j.GetProperty("status")} {j.GetProperty("progress")}%"));
Console.WriteLine(job.Json);
