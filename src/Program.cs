using System;
using System.Threading;

/// <summary>
/// The main class.
/// </summary>
public static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        var nes = new NES();
        //nes.Load("cv2", 1);
        nes.Load("mario", 1);
        //nes.Load("mario3", 4);
        nes.Power();

        var nesThread = new Thread(nes.StepForever);
        nesThread.Priority = ThreadPriority.Highest;
        nesThread.Start();

        using (var game = new Screen(nes))
            game.Run();
    }
}