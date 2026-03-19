using Spectre.Console;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Game.Services
{
    public class DownloadTask
    {
        public string Url { get; set; }
        public string SavePath { get; set; }
        public Action Completed { get; set; }
        public IProgress<int> Progress { get; set; }
    }
    public class DownloadManager
    {
        private readonly ConcurrentQueue<DownloadTask> _queue = new ConcurrentQueue<DownloadTask>();
        private readonly HttpClient _http;
        private SemaphoreSlim _semaphore;
        private bool _running;
        public ConcurrentQueue<(int id, DownloadTask task)> PendingTasks = new ConcurrentQueue<(int, DownloadTask)>();
        private int _idCounter = 0;
        public DownloadManager()
        {
            var handler = new HttpClientHandler
            {
                MaxConnectionsPerServer = 20,
                ServerCertificateCustomValidationCallback =
                    (msg, cert, chain, errors) => true
            };
            _http = new HttpClient(handler);
            _http.Timeout = TimeSpan.FromMinutes(30);
        }
        public void AddDownload(string url, string savePath)
        {
            int id = Interlocked.Increment(ref _idCounter);

            var task = new DownloadTask
            {
                Url = url,
                SavePath = savePath
            };

            PendingTasks.Enqueue((id, task));
        }
        public void Start(int maxWorkers = 3)
        {
            if (_running) return;
            _running = true;
            _semaphore = new SemaphoreSlim(maxWorkers);
            _ = Task.Run(ProcessQueue);
        }

        public void Enqueue(DownloadTask task)
        {
            if (string.IsNullOrEmpty(Path.GetExtension(task.SavePath)))
            {
                var fileName = Path.GetFileName(new Uri(task.Url).LocalPath);
                if (string.IsNullOrEmpty(fileName))
                    fileName = "download.bin";

                task.SavePath = Path.Combine(task.SavePath, fileName);
            }

            var dir = Path.GetDirectoryName(task.SavePath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            FileStream fs = null;

            while (fs == null)
            {
                try
                {
                    var uniquePath = GetUniqueFilePath(task.SavePath);
                    fs = new FileStream(uniquePath, FileMode.CreateNew, FileAccess.Write);
                    task.SavePath = uniquePath;
                }
                catch (IOException)
                {
                }
            }

            fs.Dispose();

            _queue.Enqueue(task);
        }

        private async Task ProcessQueue()
        {
            while (_running)
            {
                if (_queue.TryDequeue(out var task))
                {
                    await _semaphore.WaitAsync();
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await DownloadFile(task, 6);
                        }
                        catch (Exception ex)
                        {
                            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
                        }
                        finally
                        {
                            _semaphore.Release();
                        }
                    });
                }
                else
                {
                    await Task.Delay(100);
                }
            }
        }

        private string GetUniqueFilePath(string path)
        {
            if (!File.Exists(path)) return path;

            var dir = Path.GetDirectoryName(path);
            var name = Path.GetFileNameWithoutExtension(path);
            var ext = Path.GetExtension(path);
            int i = 1;
            while (true)
            {
                var newPath = Path.Combine(dir, $"{name}({i}){ext}");
                if (!File.Exists(newPath)) return newPath;
                i++;
            }
        }

        private async Task DownloadFile(DownloadTask task, int threads = 4)
        {

            long totalSize;
            using (var headReq = new HttpRequestMessage(HttpMethod.Head, task.Url))
            using (var headResp = await _http.SendAsync(headReq))
            {
                totalSize = headResp.Content.Headers.ContentLength ?? -1;
                if (!headResp.Headers.AcceptRanges.Contains("bytes") || totalSize <= 0)
                {
                    await SingleThreadDownload(task);
                    return;
                }
            }
            long existingLength = 0;
            if (File.Exists(task.SavePath))
            {
                existingLength = new FileInfo(task.SavePath).Length;
                if (existingLength >= totalSize)
                {
                    task.Progress?.Report(100);
                    task.Completed?.Invoke();
                    return;
                }
            }

            using (var fs = new FileStream(task.SavePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite, 8192, FileOptions.Asynchronous))
            {
                if (fs.Length < totalSize)
                    fs.SetLength(totalSize);

                threads = (int)System.Math.Min(threads, totalSize);
                var partSize = totalSize / threads;
                var progress = new DownloadProgress { TotalDownloaded = existingLength };

                var tasks = new List<Task>(threads);

                for (int i = 0; i < threads; i++)
                {
                    long start = partSize * i;
                    long end = (i == threads - 1) ? totalSize - 1 : start + partSize - 1;

                    if (existingLength > start)
                        start = existingLength;

                    if (start > end) continue;

                    tasks.Add(DownloadChunk(task, start, end, totalSize, progress));
                }

                await Task.WhenAll(tasks);
            }

            task.Progress?.Report(100);
            task.Completed?.Invoke();
        }

        private class DownloadProgress
        {
            public long TotalDownloaded;
            public int LastPercent = 0;
        }

        private async Task DownloadChunk(DownloadTask task, long start, long end, long totalSize, DownloadProgress progress)
        {
            long localDownloaded = 0;

            for (int retry = 0; retry < 3; retry++)
            {
                try
                {
                    var req = new HttpRequestMessage(HttpMethod.Get, task.Url);
                    req.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(start, end);

                    using (var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead))
                    {
                        resp.EnsureSuccessStatusCode();

                        using (var stream = await resp.Content.ReadAsStreamAsync())
                        using (var fs = new FileStream(task.SavePath, FileMode.Open, FileAccess.Write, FileShare.Write, 8192, FileOptions.Asynchronous))
                        {
                            fs.Seek(start, SeekOrigin.Begin);

                            var buffer = new byte[8192];
                            int read;
                            long pos = start;

                            while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                await fs.WriteAsync(buffer, 0, read);

                                pos += read;
                                localDownloaded += read;

                                Interlocked.Add(ref progress.TotalDownloaded, read);

                                int percent = (int)((progress.TotalDownloaded * 100) / totalSize);
                                int old = Interlocked.Exchange(ref progress.LastPercent, percent);

                                if (percent != old)
                                {
                                    task.Progress?.Report(percent);
                                }
                            }
                        }
                    }

                    break;
                }
                catch (Exception ex)
                {
                    Interlocked.Add(ref progress.TotalDownloaded, -localDownloaded);
                    localDownloaded = 0;

                    if (retry == 2)
                    {
                        //Console.WriteLine($"Chunk {start}-{end} failed after 3 retries: {ex.Message}");
                        throw;
                    }

                    //Console.WriteLine($"Retry chunk {start}-{end}, attempt {retry + 1}");

                    await Task.Delay(1000);
                }
            }
        }

        private async Task SingleThreadDownload(DownloadTask task)
        {
            using (var response = await _http.GetAsync(task.Url, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();

                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var fs = new FileStream(task.SavePath, FileMode.Open, FileAccess.Write, FileShare.None, 8192, FileOptions.Asynchronous))
                {
                    var buffer = new byte[8192];
                    int read;
                    long totalRead = 0;
                    var total = response.Content.Headers.ContentLength ?? -1L;

                    while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fs.WriteAsync(buffer, 0, read);
                        totalRead += read;

                        if (total > 0)
                        {
                            int percent = (int)((totalRead * 100) / total);
                            task.Progress?.Report(percent); 
                        }
                    }
                }
            }

            task.Completed?.Invoke();
        }
    }
}