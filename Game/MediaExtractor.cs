using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Game.Ultis
{
    public enum UrlType
    {
        Image,
        Video,
        Embed,
        Other
    }

    public class UrlInfo
    {
        public string Url { get; set; }
        public UrlType Type { get; set; }
    }

    public class MediaExtractor
    {
        private static readonly HttpClient _http = new HttpClient(
        new HttpClientHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
            AllowAutoRedirect = true,
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        })
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        private static readonly Regex _urlRegex =
            new Regex(@"((https?:)?\/\/[^\s""']+|([\w-]+\.)+[\w-]+\/[^\s""']+)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private readonly HashSet<string> _imageExt = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".svg"
        };

        private readonly HashSet<string> _videoExt = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".mp4", ".webm", ".ogg", ".mov", ".mkv", ".m3u8"
        };
        static MediaExtractor()
        {
            _http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/120 Safari/537.36");
            _http.DefaultRequestHeaders.Referrer = new Uri("https://google.com");
            _http.DefaultRequestHeaders.TryAddWithoutValidation("Accept",
                "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");

            _http.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language",
                "en-US,en;q=0.9");
            _http.DefaultRequestHeaders.ConnectionClose = true;
        }
        public async Task<List<UrlInfo>> ExtractAsync(string url)
        {
            var result = new List<UrlInfo>();
            var unique = new HashSet<string>();
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                if (Uri.TryCreate("https://" + url, UriKind.Absolute, out uri))
                {
                }
                else
                {
                    throw new Exception("Invalid URL: " + url);
                }
            }
            string new_url = uri.ToString();
            string html = await _http.GetStringAsync(new_url);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            if (doc.DocumentNode == null) return result;

            await ExtractFromScripts(doc, new_url, unique, result, 0);

            await TraverseIterative(doc.DocumentNode, new_url, unique, result, 0);

            return result;
        }
        private async Task Process(string rawUrl, string baseUrl, HashSet<string> unique, List<UrlInfo> result, int depth = 0)
        {
            if (depth > 2) return;
            if (string.IsNullOrWhiteSpace(rawUrl)) return;

            rawUrl = rawUrl.Replace("\\/", "/");
            rawUrl = rawUrl.Trim('\'', '"', ' ', '\n', '\r', '\t');

            if (rawUrl.Contains(" ") || rawUrl.Contains("<") || rawUrl.Contains(">"))
                return;

            try
            {
                if (rawUrl.StartsWith("#")) return;
                if (rawUrl.StartsWith("javascript", StringComparison.OrdinalIgnoreCase)) return;
                if (rawUrl.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)) return;
                if (rawUrl.StartsWith("tel:", StringComparison.OrdinalIgnoreCase)) return;
                if (rawUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) return;
                if (rawUrl.StartsWith("blob:", StringComparison.OrdinalIgnoreCase)) return;

                if (rawUrl.StartsWith("//"))
                {
                    rawUrl = "https:" + rawUrl;
                }

                if (!rawUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase) &&
                    Regex.IsMatch(rawUrl, @"^[a-zA-Z0-9\-]+\.[a-zA-Z]{2,}"))
                {
                    rawUrl = "https://" + rawUrl;
                }

                if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
                    return;

                if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out var uri) &&
                    !Uri.TryCreate(baseUri, rawUrl, out uri))
                    return;

                rawUrl = uri.GetLeftPart(UriPartial.Path) + uri.Query;
                var compareKey = uri.GetLeftPart(UriPartial.Path).ToLower();

                if (!unique.Add(compareKey)) return;
                var type = DetectType(rawUrl);
                if (type == UrlType.Embed)
                {
                    var real = await ResolveEmbedAsync(rawUrl);

                    if (real.Type == UrlType.Video)
                    {
                        result.Add(real);
                        return;
                    }
                }
                result.Add(new UrlInfo
                {
                    Url = rawUrl,
                    Type = type
                });
            }
            catch
            {
            }
        }
        public async Task<UrlInfo> ResolveEmbedAsync(string embedUrl)
        {
            if (embedUrl.Contains("youtube.com/embed"))
            {
                var match = Regex.Match(embedUrl, @"youtube\.com\/embed\/([a-zA-Z0-9_-]+)");
                if (match.Success)
                {
                    var videoId = match.Groups[1].Value;

                    return new UrlInfo
                    {
                        Url = $"https://www.youtube.com/watch?v={videoId}",
                        Type = UrlType.Video
                    };
                }
            }
            try
            {
                string html = await _http.GetStringAsync(embedUrl);

                var baseMatch = Regex.Match(html,
                    @"video_cdn_url_base\s*=\s*['""](?<base>[^'""]+)['""]",
                    RegexOptions.IgnoreCase);

                var baseUrl = baseMatch.Success ? baseMatch.Groups["base"].Value : "";

                var m3u8Match = Regex.Match(html,
                    @"video_url_1080\s*=\s*['""](?<url>[^'""]+)['""]",
                    RegexOptions.IgnoreCase);
                if (m3u8Match.Success && !string.IsNullOrEmpty(m3u8Match.Groups["url"].Value))
                {
                    var path = m3u8Match.Groups["url"].Value;

                    var full = path.StartsWith("http")
                        ? path
                        : baseUrl + path;

                    return new UrlInfo
                    {
                        Url = full,
                        Type = UrlType.Video
                    };
                }
                var hls = Regex.Match(html, @"https?:\/\/[^""']+\.m3u8");
                var mp4 = Regex.Match(html, @"https?:\/\/[^""']+\.mp4");

                if (hls.Success)
                    return new UrlInfo { Url = hls.Value, Type = UrlType.Video };

                if (mp4.Success)
                    return new UrlInfo { Url = mp4.Value, Type = UrlType.Video };

                var mp4Match = Regex.Match(html,
                    @"video_url_720\s*=\s*['""](?<url>[^'""]+)['""]",
                    RegexOptions.IgnoreCase);

                if (mp4Match.Success && !string.IsNullOrEmpty(mp4Match.Groups["url"].Value))
                {
                    var path = mp4Match.Groups["url"].Value;

                    var full = path.StartsWith("http")
                        ? path
                        : baseUrl + path;

                    return new UrlInfo
                    {
                        Url = full,
                        Type = UrlType.Video
                    };
                }

                var fallback = Regex.Match(html, @"https?:\/\/[^""']+\.(mp4|m3u8)",
                    RegexOptions.IgnoreCase);

                if (fallback.Success)
                {
                    return new UrlInfo
                    {
                        Url = fallback.Value,
                        Type = UrlType.Video
                    };
                }
            }
            catch
            {
            }

            return new UrlInfo
            {
                Url = embedUrl,
                Type = UrlType.Other
            };
        }
        private static readonly HashSet<string> ImportantAttrs = new HashSet<string>
        {
            "src", "href", "data-src", "data-vid", "poster", "xmlns", "data-href", "onclick", "lang", "data-original", "alt", "content", "style", "data-urlimg", "itemtype", "data-srcset", "data-url", "data-cache-file-woff2", "data-cache-file-woff", "data-cache-file-ttf", "srcset"
        };
        private async Task TraverseIterative(HtmlNode root, string baseUrl, HashSet<string> unique, List<UrlInfo> result, int depth = 0)
        {
            var stack = new Stack<HtmlNode>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                var node = stack.Pop();
                if (node.Name == "iframe")
                {
                    var src = node.GetAttributeValue("src", "");
                    if (!string.IsNullOrEmpty(src))
                    {
                        await Process(src, baseUrl, unique, result, depth + 1);
                    }
                }
                if (node.HasAttributes)
                {
                    await ScanAttributes(node, baseUrl, unique, result, depth);
                }

                foreach (var child in node.ChildNodes)
                {
                    stack.Push(child);
                }
            }
        }
        private async Task ScanAttributes(HtmlNode node, string baseUrl, HashSet<string> unique, List<UrlInfo> result, int depth)
        {
            foreach (var attr in node.Attributes)
            {
                //if (!ImportantAttrs.Contains(attr.Name))
                //    continue;
                var value = attr.Value;
                if (string.IsNullOrWhiteSpace(value)) continue;

                value = value.Trim();

                if (value.StartsWith("http") || value.StartsWith("//"))
                {
                    await Process(value, baseUrl, unique, result, depth);
                    continue;
                }
                if (value.Contains("/") || value.Contains("."))
                {
                    foreach (Match match in _urlRegex.Matches(value))
                    {
                        await Process(match.Value, baseUrl, unique, result, depth);
                    }
                }
            }
        }
        private async Task ExtractFromScripts(HtmlDocument doc, string baseUrl, HashSet<string> unique, List<UrlInfo> result, int depth)
        {
            var scripts = doc.DocumentNode.SelectNodes("//script");

            if (scripts == null) return;

            foreach (var node in scripts)
            {
                var text = System.Net.WebUtility.HtmlDecode(node.InnerText);

                if (string.IsNullOrWhiteSpace(text)) continue;

                foreach (Match match in _urlRegex.Matches(text))
                {
                    await Process(match.Value, baseUrl, unique, result, depth);
                }
            }
        }
        private UrlType DetectType(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return UrlType.Other;

            var path = uri.AbsolutePath.ToLowerInvariant();
            var ext = Path.GetExtension(path);

            if (_videoExt.Contains(ext))
                return UrlType.Video;
            if (_imageExt.Contains(ext))
                return UrlType.Image;
            if (path.Contains("/embed/") || path.Contains("/playback/") || path.Contains("/iframe/"))
                return UrlType.Embed;

            return UrlType.Other;
        }
    }
}