using System;
using System.Windows;

namespace CarelessWhisperV2
{
    public class TestMinimal
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Console.WriteLine("MINIMAL TEST: Starting...");
            
            try
            {
                Console.WriteLine("MINIMAL TEST: Creating WPF Application...");
                var app = new Application();
                
                Console.WriteLine("MINIMAL TEST: Creating basic window...");
                var window = new Window
                {
                    Title = "Minimal Test",
                    Width = 300,
                    Height = 200,
                    WindowState = WindowState.Minimized,
                    ShowInTaskbar = false
                };
                
                Console.WriteLine("MINIMAL TEST: Hiding window...");
                window.Hide();
                
                Console.WriteLine("MINIMAL TEST: Running application...");
                app.Run(window);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MINIMAL TEST ERROR: {ex.Message}");
                Console.WriteLine($"MINIMAL TEST STACK: {ex.StackTrace}");
            }
        }
    }
}
