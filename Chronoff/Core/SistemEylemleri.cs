using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Chronoff.Core
{
    #region Sistem Kapatma, Kilitleme ve Uyku Eylemleri (System Power Actions & Lock)

    /// <summary>
    /// Uygulamanın gerçekleştirebileceği sistem eylemlerini tanımlar.
    /// </summary>
    public enum SistemGorevi
    {
        Kapat,
        YenidenBaslat,
        Sleep,
        Hibernate,
        Kilitle,
        OturumuKapat
    }

    /// <summary>
    /// Yineleme tiplerini tanımlar (Günlük, Haftalık, Aylık).
    /// </summary>
    public enum RecurrenceType
    {
        Daily = 0,
        Weekly = 1,
        Monthly = 2
    }

    public static class SistemEylemleri
    {
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool LockWorkStation();

        public static void EylemiGerceklestir(SistemGorevi gorev)
        {
            if (gorev == SistemGorevi.Kilitle)
            {
                if (!LockWorkStation())
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
                System.Windows.Application.Current.Shutdown();
                System.Environment.Exit(0);
                return;
            }

            string system32 = Environment.GetFolderPath(Environment.SpecialFolder.System);
            string shutdownYolu = System.IO.Path.Combine(system32, "shutdown.exe");
            string rundllYolu = System.IO.Path.Combine(system32, "rundll32.exe");

            string dosyaAdi = shutdownYolu;
            string argumanlar = "/s /t 0";

            switch (gorev)
            {
                case SistemGorevi.YenidenBaslat: dosyaAdi = shutdownYolu; argumanlar = "/r /t 0"; break;
                case SistemGorevi.OturumuKapat: dosyaAdi = shutdownYolu; argumanlar = "/l"; break;
                case SistemGorevi.Sleep: dosyaAdi = rundllYolu; argumanlar = "powrprof.dll,SetSuspendState 0,1,0"; break;
                case SistemGorevi.Hibernate: dosyaAdi = shutdownYolu; argumanlar = "/h"; break;
                case SistemGorevi.Kapat:
                default: dosyaAdi = shutdownYolu; argumanlar = "/s /t 0"; break;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = dosyaAdi,
                    Arguments = argumanlar,
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
            }
            catch (Exception hata)
            {
                throw new InvalidOperationException($"'{dosyaAdi} {argumanlar}' başlatılamadı.", hata);
            }

            System.Windows.Application.Current.Shutdown();
            System.Environment.Exit(0);
        }
    }

    #endregion
}
