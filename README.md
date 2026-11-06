# HuntAPI .NET SDK

The official .NET SDK for the [HuntAPI](https://www.huntapi.com) media download API.

Download video and audio from public media URLs, or extract their metadata, with one async REST API.

The SDK exposes **6 typed async methods**, one per documented endpoint (with `required` properties checked at compile time), plus a generic `RequestAsync`.

## Requirements

- .NET 8 or newer

## Installation

```sh
dotnet add package HuntApi
```

## Get your API key

[Sign up for HuntAPI](https://app.huntapi.com/register) or sign in, then copy your API key from the [dashboard](https://app.huntapi.com). `new HuntApiClient()` reads the `HUNTAPI_API_KEY` environment variable.

## Quick start

```csharp
using HuntApi;

using var client = new HuntApiClient();
var response = await client.MediaMetadataAsync(new MediaMetadataParams { Query = "https://www.youtube.com/watch?v=Z5YP8kznWZs" });
Console.WriteLine(response.Json);
```

Parameter classes mirror the [API reference](https://docs.huntapi.com). Use `response.Deserialize<MyType>()` to map the JSON onto your own types.

## Async jobs

Download endpoints are asynchronous: they return a `job_id`, and jobs can run for up to 12 hours. `WaitForJobAsync` polls `GET /v1/jobs/{id}` until a terminal status (`CompletedJob`, `Error`, `NotFound`, `BadRequest`):

```csharp
var submitted = await client.VideoDownloadAsync(new VideoDownloadParams { Query = "https://www.youtube.com/watch?v=Z5YP8kznWZs", VideoQuality = "720p" });
var job = await client.WaitForJobAsync(
    submitted.Json.GetProperty("job_id").GetString()!,
    onProgress: j => Console.WriteLine($"{j.GetProperty("status")} {j.GetProperty("progress")}%"));
```

## GET or POST

Methods use GET when the endpoint supports it (query string). POST sends the same parameters as JSON:

```csharp
await client.AudioDownloadAsync(new AudioDownloadParams { Query = "test" }, new RequestOptions { Method = "POST" });
```

## Any endpoint

```csharp
var response = await client.RequestAsync("GET", "/v1/usage");
```

## Errors

```csharp
try
{
    await client.MediaMetadataAsync(new MediaMetadataParams { Query = "https://www.youtube.com/watch?v=Z5YP8kznWZs" });
}
catch (HuntApiApiException exception)
{
    Console.Error.WriteLine($"{exception.StatusCode}: {exception.Message}");
}
```

| Exception | When |
| --- | --- |
| `HuntApiConfigurationException` | Missing API key, invalid options or unsupported method. |
| `HuntApiSerializationException` | A required parameter is missing or a value can't be sent. |
| `HuntApiTransportException` | Network failure or timeout. |
| `HuntApiApiException` | Non-2xx status (`StatusCode`, `ErrorMessage`, `ResponseBody`, `RawResponse`). |
| `HuntApiResponseDecodingException` | A success response could not be decoded. |

All of them derive from `HuntApiException`. Caller cancellation surfaces as `OperationCanceledException`.

## Retries

Requests are retried up to 3 times after a network error, a `408`, `500`, `502`, `503` or `504`, or a rate limit — which the API returns as `401` (or `429`) with a `Rate limit exceeded` message. Per-key quota errors (`…: quota daily`) are not retried, nor are `400`, `402`, `404` or client-side timeouts. The delay starts at 1 second and grows 1.5× per retry (capped at 30 seconds); a `Retry-After` header wins.

```csharp
using var client = new HuntApiClient(new HuntApiClientOptions { MaxRetries = 5, RetryDelay = TimeSpan.FromMilliseconds(500) });
await client.MediaMetadataAsync(new MediaMetadataParams { Query = "https://www.youtube.com/watch?v=Z5YP8kznWZs" }, new RequestOptions { MaxRetries = 0 }); // one request without retries
```

## Batch requests

`BatchAsync` runs a function per item with at most 10 in flight (`concurrency`) and returns one `BatchResult` per item, in input order. A failing item never stops the others.

```csharp
string[] inputs = { "https://www.youtube.com/watch?v=Z5YP8kznWZs", "https://www.youtube.com/watch?v=jNQXAC9IVRw" };
var results = await client.BatchAsync(inputs, (value, ct) =>
    client.MediaMetadataAsync(new MediaMetadataParams { Query = value }, cancellationToken: ct), concurrency: 5);
foreach (var result in results)
{
    if (result.Ok)
    {
        Console.WriteLine(result.Value!.Json);
    }
    else
    {
        Console.Error.WriteLine($"{inputs[result.Index]}: {result.Error!.Message}");
    }
}
```

| Constant | Default | Description |
| --- | --- | --- |
| `HuntApiClient.DefaultConcurrency` | `10` | Tasks run at the same time by batch |
| `HuntApiClient.DefaultTimeout` | `TimeSpan.FromSeconds(120)` | Request timeout |
| `HuntApiClient.MaxRetries` | `3` | Retry attempts after a network error, a 408/5xx or a rate limit |
| `HuntApiClient.RetryBackoffFactor` | `1.5` | Exponential backoff multiplier |
| `HuntApiClient.DefaultRetryDelay` | `TimeSpan.FromSeconds(1)` | Delay before the first retry |

## Configuration

```csharp
using var client = new HuntApiClient(new HuntApiClientOptions
{
    ApiKey = Environment.GetEnvironmentVariable("HUNTAPI_API_KEY"),
    Timeout = TimeSpan.FromSeconds(60), // default 120 s
    HttpClient = myHttpClient,           // optional, you keep ownership
});
```

## Endpoints

6 methods, generated from the [OpenAPI document](https://app.huntapi.com/openapi).

| Method | HTTP | Description |
| --- | --- | --- |
| `AudioDownloadAsync` | GET / POST `/v1/audio/download` | Download audio from a URL via HuntAPI and receive a job id. |
| `JobsAsync` | GET `/v1/jobs/{id}` | Poll status, progress (0–100), and result of an async HuntAPI job. |
| `MediaMetadataAsync` | GET `/v1/media/metadata` | Extract metadata from a media URL without downloading the file. |
| `StatsAsync` | GET `/v1/stats` | Infrastructure health for an engine or endpoint over the last hour. Not scoped to your account or tenant. Enabled per account: HTTP 403 mea… |
| `UsageAsync` | GET `/v1/usage` | Retrieve real-time usage statistics and credit balance for your Huntapi account. |
| `VideoDownloadAsync` | GET / POST `/v1/video/download` | Video Download |

## All HuntAPI SDKs

| SDK | Package | Install |
| --- | --- | --- |
| Node.js | [`@huntapi/sdk`](https://www.npmjs.com/package/@huntapi/sdk) | `npm install @huntapi/sdk` |
| Python | [`huntapi`](https://pypi.org/project/huntapi/) | `pip install huntapi` |
| Go | [`huntapi-go`](https://pkg.go.dev/github.com/huntapi/huntapi-go) | `go get github.com/huntapi/huntapi-go` |
| .NET | [`HuntApi`](https://www.nuget.org/packages/HuntApi) | `dotnet add package HuntApi` |
| Ruby | [`huntapi`](https://rubygems.org/gems/huntapi) | `gem install huntapi` |
| PHP | [`huntapi/sdk`](https://packagist.org/packages/huntapi/sdk) | `composer require huntapi/sdk` |
| Java | [`huntapi-java`](https://github.com/huntapi/huntapi-java) | build from source, see its README |
| CLI | [`@huntapi/cli`](https://www.npmjs.com/package/@huntapi/cli) | `npm install -g @huntapi/cli` |
| MCP server | [`@huntapi/mcp`](https://www.npmjs.com/package/@huntapi/mcp) | `npx -y @huntapi/mcp` |

## License

MIT — see [LICENSE](LICENSE).
