using Game.Core;
using System;
class Program
{
    static void Main()
    {
        Console.Title = "Turn-Based Game Demo";
        try
        {
            GameManager.Instance.Init();

            GameManager.Instance.Start();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Game crashed!");
            Console.WriteLine(ex.Message);
            Console.ReadKey();
        }
    }
}