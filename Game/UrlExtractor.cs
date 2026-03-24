using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Game.Ultis
{
    public enum UrlType
    {
        Image,
        Video,
        Other
    }

    public class UrlInfo
    {
        public string Url { get; set; }
        public UrlType Type { get; set; }
    }

    public class UrlExtractor
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
        private readonly HashSet<string> _imageExt = new HashSet<string>
        {
            ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".svg"
        };

        private readonly HashSet<string> _videoExt = new HashSet<string>
        {
            ".mp4", ".webm", ".ogg", ".mov", ".mkv", ".m3u8"
        };
        public async Task<List<UrlInfo>> ExtractAsync(string url)
        {
            var result = new List<UrlInfo>();
            var unique = new HashSet<string>();
            string html = await _http.GetStringAsync(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            if (doc.DocumentNode == null) return result;

            ExtractFromScripts(doc, url, unique, result);

            TraverseIterative(doc.DocumentNode, url, unique, result);

            return result;
        }
        private void Process(string rawUrl, string baseUrl, HashSet<string> unique, List<UrlInfo> result)
        {
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

                Uri baseUri;
                if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out baseUri))
                    return;

                Uri uri;
                if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out uri))
                {
                    if (!Uri.TryCreate(baseUri, rawUrl, out uri))
                        return;
                }

                rawUrl = uri.GetLeftPart(UriPartial.Path) + uri.Query;
                var compareKey = uri.GetLeftPart(UriPartial.Path).ToLower();

                if (!unique.Add(compareKey)) return;

                result.Add(new UrlInfo
                {
                    Url = rawUrl,
                    Type = DetectType(rawUrl)
                });
            }
            catch
            {
            }
        }
        private static readonly HashSet<string> ImportantAttrs = new HashSet<string>
        {
            "src", "href", "data-src", "data-vid", "poster", "xmlns", "data-href", "onclick", "lang", "data-original", "alt", "content", "style", "data-urlimg", "itemtype", "data-srcset", "data-url", "data-cache-file-woff2", "data-cache-file-woff", "data-cache-file-ttf", "srcset"
        };
        private void TraverseIterative(HtmlNode root, string baseUrl, HashSet<string> unique, List<UrlInfo> result)
        {
            var stack = new Stack<HtmlNode>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                var node = stack.Pop();
                if (node.HasAttributes)
                {
                    ScanAttributes(node, baseUrl, unique, result);
                }

                foreach (var child in node.ChildNodes)
                {
                    stack.Push(child);
                }
            }
        }
        private void ScanAttributes(HtmlNode node, string baseUrl, HashSet<string> unique, List<UrlInfo> result)
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
                    Process(value, baseUrl, unique, result);
                    continue;
                }
                if (value.Contains("/") || value.Contains("."))
                {
                    foreach (Match match in _urlRegex.Matches(value))
                    {
                        Process(match.Value, baseUrl, unique, result);
                    }
                }
            }
        }
        private void ExtractFromScripts(HtmlDocument doc, string baseUrl, HashSet<string> unique, List<UrlInfo> result)
        {
            var scripts = doc.DocumentNode.SelectNodes("//script");

            if (scripts == null) return;

            foreach (var node in scripts)
            {
                var text = System.Net.WebUtility.HtmlDecode(node.InnerText);

                if (string.IsNullOrWhiteSpace(text)) continue;

                foreach (Match match in _urlRegex.Matches(text))
                {
                    Process(match.Value, baseUrl, unique, result);
                }
            }
        }
        private Dictionary<string, string> ParseQuery(string query)
        {
            return query.TrimStart('?')
                .Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Split(new[] { '=' }, 2))
                .ToDictionary(
                    x => x[0],
                    x => x.Length > 1 ? Uri.UnescapeDataString(x[1]) : ""
                );
        }
        private UrlType DetectType(string url)
        {
            try
            {
                var uri = new Uri(url);

                var queryDict = ParseQuery(uri.Query);

                if (queryDict.TryGetValue("fileId", out var fileId))
                {
                    var decoded = fileId.ToLower();

                    if (_videoExt.Any(ext => decoded.Contains(ext)))
                        return UrlType.Video;
                }

                var path = uri.AbsolutePath.ToLower();

                if (_videoExt.Any(ext => path.EndsWith(ext)))
                    return UrlType.Video;

                if (_imageExt.Any(ext => path.EndsWith(ext)))
                    return UrlType.Image;
            }
            catch { }

            return UrlType.Other;
        }
    }
}