using Csv;
using Game.Services;
using Game.Ultis;
using HtmlAgilityPack;
using Spectre.Console;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static void Main()
    {
        //var csvLogger = new CsvLogger("C:\\D\\Download\\download_log.csv");
        //var downloader = new DownloadManager(csvLogger);
        //const int maxWorkers = 100;
        //downloader.Start(maxWorkers);
        //const int maxVisible = maxWorkers;
        //var logQueue = new ConcurrentQueue<string>();
        //var uiTask = Task.Run(() =>
        //{
        //    AnsiConsole.Progress()
        //    .Start(ctx =>
        //    {
        //        var slots = new ProgressTask[maxVisible];
        //        var slotBusy = new bool[maxVisible];

        //        var slotLocks = new object[maxVisible];

        //        for (int i = 0; i < maxVisible; i++)
        //        {
        //            slots[i] = ctx.AddTask("[grey]Waiting...[/]");
        //            slotLocks[i] = new object();
        //        }
        //        while (true)
        //        {
        //            bool hasWork =
        //                !downloader.PendingTasks.IsEmpty ||
        //                Array.Exists(slotBusy, b => b);

        //            if (!hasWork)
        //            {
        //                Thread.Sleep(100);
        //                continue;
        //            }

        //            for (int i = 0; i < maxVisible; i++)
        //            {
        //                if (slotBusy[i]) continue;

        //                if (downloader.PendingTasks.TryDequeue(out var item))
        //                {
        //                    int slot = i;

        //                    int id = item.id;
        //                    var task = item.task;

        //                    var spectreTask = slots[slot];
        //                    slotBusy[slot] = true;
        //                    lock (slotLocks[slot])
        //                    {
        //                        slotBusy[slot] = true;
        //                        spectreTask.Description = $"[green]File {id}[/]";
        //                        spectreTask.Value = 0;
        //                    }

        //                    task.Progress = new Progress<int>(p =>
        //                    {
        //                        lock (slotLocks[slot])
        //                        {
        //                            spectreTask.Value = p;
        //                        }
        //                    });

        //                    task.Completed = () =>
        //                    {
        //                        lock (slotLocks[slot])
        //                        {
        //                            spectreTask.Value = 100;
        //                            spectreTask.StopTask();
        //                            spectreTask.Description = "[grey]Waiting...[/]";
        //                            slotBusy[slot] = false;
        //                        }

        //                        logQueue.Enqueue($"[green] File {id} done[/]");
        //                    };

        //                    downloader.Enqueue(task);
        //                }
        //            }

        //            while (logQueue.TryDequeue(out var log))
        //            {
        //                AnsiConsole.MarkupLine(log);
        //            }

        //            Thread.Sleep(100);
        //        }
        //    });
        //});
        //for (int i = 0; i < 100; i++)
        //{
        //    downloader.AddDownload(
        //        "https://data.bsoftjsc.com/uploads/BVRRecPro_1.0.1-release.apk",
        //        @"C:\\D\\Download\\"
        //    );
        //}
        List<string> urls_list = new List<string>
        {
        "https://media.congly.vn/bo-da-tram-tich-mot-mien-thien-tu-516441.html",
        "https://tienphong.vn/my-no-lon-tai-nha-may-loc-dau-dung-thoi-diem-gia-xang-dau-tang-cao-post1829881.tpo",
        "https://nangluongquocte.petrotimes.vn/chay-lon-tai-nha-may-loc-dau-bang-texas-cua-my-739021.html",
        "https://www.saostar.vn/sac-mau-cuoc-song/be-gai-hoang-loan-la-het-trong-con-ngo-vang-nguoi-camera-ghi-lai-gi-202603241319110062.html",
        "https://plo.vn/khi-nao-nen-an-chuoi-de-tiep-nang-luong-nhanh-va-ben-nhat-post900815.html",
        "https://plo.vn/chay-quan-an-o-trung-tam-da-lat-may-man-khong-co-thiet-hai-ve-nguoi-post900871.html",
        "https://baomoi.com/israel-danh-sap-trai-tim-ten-lua-iran-phan-cong-suot-10-gio-c54773050.epi",
        "https://hoahoctro.tienphong.vn/moana-live-action-tung-trailer-a-than-maui-va-cua-khong-lo-ai-bi-che-nhat-post1829987.tpo"
        };
        Task.Run(async () =>
        {
            var extractor = new MediaExtractor();

            var urls = await extractor.ExtractAsync(
                "https://plo.vn/khi-nao-nen-an-chuoi-de-tiep-nang-luong-nhanh-va-ben-nhat-post900815.html"
            );

            var filtered = urls
                .Where(x => x.Type == UrlType.Image || x.Type == UrlType.Video)
                .ToList();

            var basePath = @"C:\D\Download\url_log";

            var dir = Path.GetDirectoryName(basePath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            using (var jsonStream = new FileStream(basePath + ".json", FileMode.Create))
            {
                await JsonSerializer.SerializeAsync(jsonStream, filtered, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
            }

            var headers = new[] { "Url", "Type" };

            var rows = filtered.Select(x => new[]
            {
                x.Url,
                " "+ x.Type.ToString()
            });

            var csv = CsvWriter.WriteToText(headers, rows);

            File.WriteAllText(basePath + ".csv", csv, new UTF8Encoding(true));

        }).GetAwaiter().GetResult();
        //Console.ReadLine();
        //Console.Title = "Turn-Based Game Demo"; 
        //try 
        //{ 
        // GameManager.Instance.Init(); 
        // GameManager.Instance.Start(); 
        //} 
        //catch (Exception ex) 
        //{ 
        // Console.WriteLine("Game crashed!"); 
        // Console.WriteLine(ex.Message); 
        // Console.ReadKey(); 
        //}
    }
}