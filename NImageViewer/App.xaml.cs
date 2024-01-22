using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace NImageViewer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            this.Dispatcher.UnhandledException += OnDispatcherUnhandledException;
        }

        void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            if (e.Exception is OverflowException)
            {
                NImageViewer.MainWindow.WasOverflow = true;
                string errorMessage = string.Format("An unhandled exception occurred: {0}", e.Exception.Message);
                Trace.TraceError(errorMessage, e);
                e.Handled = true;
            }
        }
    }
}
