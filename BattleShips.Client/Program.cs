using System;
using System.IO;

namespace BattleShips.Client
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                // Find and load .env by traversing up from the exe folder (bin/... -> project root)
                DotNetEnv.Env.TraversePath().Load();
            }
            catch {
                Console.WriteLine("Error loading .env file");
             }

            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }
    }
}