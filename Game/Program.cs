using Game.Services;
using Spectre.Console;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static void Main()
    {
        var downloader = new DownloadManager();
        downloader.Start(5);

        const int maxVisible = 5;

        var logQueue = new ConcurrentQueue<string>();

        var uiTask = Task.Run(() =>
        {
            AnsiConsole.Progress()
            .Start(ctx =>
            {
                var slots = new ProgressTask[maxVisible];
                var slotBusy = new bool[maxVisible];

                var slotLocks = new object[maxVisible];

                for (int i = 0; i < maxVisible; i++)
                {
                    slots[i] = ctx.AddTask("[grey]Waiting...[/]");
                    slotLocks[i] = new object();
                }

                while (true)
                {
                    bool hasWork =
                        !downloader.PendingTasks.IsEmpty ||
                        Array.Exists(slotBusy, b => b);

                    if (!hasWork)
                    {
                        Thread.Sleep(100);
                        continue;
                    }

                    for (int i = 0; i < maxVisible; i++)
                    {
                        if (slotBusy[i]) continue;

                        if (downloader.PendingTasks.TryDequeue(out var item))
                        {
                            int slot = i;

                            int id = item.id;
                            var task = item.task;

                            var spectreTask = slots[slot];
                            slotBusy[slot] = true;
                            lock (slotLocks[slot])
                            {
                                slotBusy[slot] = true;
                                spectreTask.Description = $"[green]File {id}[/]";
                                spectreTask.Value = 0;
                            }

                            task.Progress = new Progress<int>(p =>
                            {
                                lock (slotLocks[slot])
                                {
                                    spectreTask.Value = p;
                                }
                            });

                            task.Completed = () =>
                            {
                                lock (slotLocks[slot])
                                {
                                    spectreTask.Value = 100;
                                    spectreTask.StopTask();
                                    spectreTask.Description = "[grey]Waiting...[/]";
                                    slotBusy[slot] = false;
                                }

                                logQueue.Enqueue($"[green] File {id} done[/]");
                            };

                            downloader.Enqueue(task);
                        }
                    }

                    while (logQueue.TryDequeue(out var log))
                    {
                        AnsiConsole.MarkupLine(log);
                    }

                    Thread.Sleep(100);
                }
            });
        });
        for (int i = 0; i < 100; i++)
        {
            downloader.AddDownload(
                "https://data.bsoftjsc.com/uploads/BVRRecPro_1.0.1-release.apk",
                @"C:\D\Download\"
            );
        }
        Console.ReadLine();
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