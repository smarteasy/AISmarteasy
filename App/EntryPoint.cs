using System;
using System.Windows;

namespace GPTApp
{
    public static class EntryPoint
    {
        [STAThread]
        public static void Main()
        {
            RunMainForm();
        }

        private static void RunMainForm()
        {
            var app = new Application();
            var mainWindow = new MainWindow();
            app.Run(mainWindow);
        }

    }
}