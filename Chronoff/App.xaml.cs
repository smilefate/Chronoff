using System;
using System.Threading;
using System.Windows;

namespace Chronoff
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private static Mutex? _mutex;

        protected override void OnStartup(StartupEventArgs e)
        {
            const string mutexName = "Global\\Chronoff_SingleInstance_Mutex_987654";
            _mutex = new Mutex(true, mutexName, out bool createdNew);

            if (!createdNew)
            {
                System.Windows.MessageBox.Show(
                    "Chronoff zaten arka planda çalışıyor.", 
                    "Chronoff", 
                    System.Windows.MessageBoxButton.OK, 
                    System.Windows.MessageBoxImage.Information);

                Shutdown();
                return;
            }

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_mutex != null)
            {
                try
                {
                    _mutex.ReleaseMutex();
                }
                catch { }
                _mutex.Dispose();
            }
            base.OnExit(e);
        }
    }
}
