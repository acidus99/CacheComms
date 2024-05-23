using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text;

namespace CacheComms;

public class HttpRequestor
{
    public byte[]? BodyBytes { get; internal set; } = null!;

    public string? BodyText { get; internal set; } = null!;

    /// <summary>
    /// How long should responses be unconditionally cached for?
    /// </summary>
    public TimeSpan CacheExpiration
    {
        get => Cache.EntryLifespan;
        set => Cache.EntryLifespan = value;
    }
        
    public long DownloadTimeMs
        => (long)RequestStopwatch.ElapsedMilliseconds;

    public string ErrorMessage { get; internal set; } = "";

    DiskCache Cache;
    HttpClient Client;
    Stopwatch RequestStopwatch;

    public HttpRequestor()
    {
        Client = new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = true,
            CheckCertificateRevocationList = false,
            AutomaticDecompression = System.Net.DecompressionMethods.All,
        });

        Cache = new DiskCache("http");

        Client.Timeout = TimeSpan.FromSeconds(20);
        RequestStopwatch = new Stopwatch();
        EmulateBrowser(Client);
    }

    [MemberNotNullWhen(true, nameof(BodyBytes))]
    public bool GetAsBytes(Uri url, bool useCache = true)
    {
        RequestStopwatch.Restart();
        if (!IsValidUrl(url))
        {
            return false;
        }

        if (useCache)
        {
            //try and get it from the cache
            string key = GetCacheKey(url);
            var content = Cache.GetAsBytes(key);

            if (content != null)
            {
                BodyBytes = content;
                RequestStopwatch.Stop();
                return true;
            }
        }

        HttpResponseMessage httpResponse;
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            httpResponse = Client.Send(request, HttpCompletionOption.ResponseContentRead);
            RequestStopwatch.Stop();
        }
        catch (TaskCanceledException)
        {
            RequestStopwatch.Stop();
            ErrorMessage = "Could not download content for URL. Connection Timeout.";
            return false;
        }
        catch (Exception ex)
        {
            RequestStopwatch.Stop();
            ErrorMessage = "Error requesting url. " + ex.Message;
            return false;
        }

        if (!httpResponse.IsSuccessStatusCode)
        {
            ErrorMessage = $"Could not download content for URL. Status code: '{httpResponse.StatusCode}'";
            return false;
        }

        BodyBytes = httpResponse.Content.ReadAsByteArrayAsync().Result;

        if (BodyBytes.Length == 0)
        {
            ErrorMessage = $"Received no binary content for '{url}'";
            return false;
        }

        if (useCache)
        {
            //ok, we are good, store this in the cache
            string key = GetCacheKey(url);
            Cache.Set(key, BodyBytes);
        }
        return true;
    }

    [MemberNotNullWhen(true, nameof(BodyText))]
    public bool GetAsString(Uri url, bool useCache = true)
    {
        RequestStopwatch.Restart();
        if (!IsValidUrl(url))
        {
            return false;
        }

        if (useCache)
        {
            //try and get it from the cache
            string key = GetCacheKey(url);
            var content = Cache.GetAsString(key);

            if (content != null)
            {
                BodyText = content;
                RequestStopwatch.Stop();
                return true;
            }
        }

        HttpResponseMessage httpResponse;
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            httpResponse = Client.Send(request, HttpCompletionOption.ResponseContentRead);
            RequestStopwatch.Stop();
        }
        catch (TaskCanceledException)
        {
            RequestStopwatch.Stop();
            ErrorMessage = "Could not download content for URL. Connection Timeout.";
            return false;
        }
        catch (Exception ex)
        {
            RequestStopwatch.Stop();
            ErrorMessage = "Error requesting url. " + ex.Message;
            return false;
        }

        if (!httpResponse.IsSuccessStatusCode)
        {
            ErrorMessage = $"Could not download content for URL. Status code: '{httpResponse.StatusCode}'";
            return false;
        }

        BodyBytes = httpResponse.Content.ReadAsByteArrayAsync().Result;
        var charset = GetCharset(httpResponse.Content.Headers.ContentType);
        BodyText = Encoding.GetEncoding(charset).GetString(BodyBytes);
        if (string.IsNullOrEmpty(BodyText))
        {
            ErrorMessage = $"Received no text content for '{url}'";
            return false;
        }

        if (useCache)
        {
            //ok, we are good, store this in the cache
            string key = GetCacheKey(url);
            Cache.Set(key, BodyText);
        }
        return true;
    }

    private void EmulateBrowser(HttpClient client)
    {
        // Use HTTP headers sent by MacOS Safari Version 17.3.1 (19617.2.4.11.12)
        client.DefaultRequestHeaders.UserAgent.TryParseAdd("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.3.1 Safari/605.1.15");
        client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
        client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
        client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
        client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "none");
    }

    private static string GetCharset(MediaTypeHeaderValue? contentType)
        => (contentType?.CharSet?.Length > 0) ?
            contentType.CharSet :
            "utf-8";

    public static bool IsValidUrl(Uri url)
        => url.IsAbsoluteUri && url.Scheme.StartsWith("http");

    private static string GetCacheKey(Uri url)
        => url.AbsoluteUri;
}
