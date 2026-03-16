using System;
namespace Game.Delegates
{
    public delegate void GameLog(string message);
    public static class Logger
    {
        public static GameLog OnLog;

        public static void Log(string message)
        {
            OnLog?.Invoke(message);
        }
    }
    public static class LogHandlers
    {
        public static void ConsoleLogger(string message)
        {
            Console.WriteLine("[LOG] " + message);
        }

        public static void FileLogger(string message)
        {
            System.IO.File.AppendAllText("gamelog.txt", message + "\n");
        }
    }
}