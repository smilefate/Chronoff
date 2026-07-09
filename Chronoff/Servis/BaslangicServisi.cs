using System;
using Microsoft.Win32;

namespace Chronoff.Servis
{
    public static class BaslangicServisi
    {
        private const string RegistryKeyName = "Chronoff";
        private const string RunRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

        /// <summary>TR: Başlangıçta çalıştırma ayarını günceller. EN: Configures Windows startup execution.</summary>
        public static void WindowsBaslangiciniAyarla(bool etkinlestir)
        {
            string? exeYolu = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exeYolu)) return;

            try
            {
                using (var rgAnahtar = Registry.CurrentUser.OpenSubKey(RunRegistryPath, true))
                {
                    if (rgAnahtar != null)
                    {
                        if (etkinlestir)
                        {
                            rgAnahtar.SetValue(RegistryKeyName, $"\"{exeYolu}\"");
                        }
                        else
                        {
                            rgAnahtar.DeleteValue(RegistryKeyName, false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Windows başlangıç ayarı yapılamadı: {ex.Message}");
            }
        }

        /// <summary>TR: Başlangıçta çalışıp çalışmadığını sorgular. EN: Reads Windows startup status.</summary>
        public static bool BaslangicDurumunuOku()
        {
            try
            {
                using (var rgAnahtar = Registry.CurrentUser.OpenSubKey(RunRegistryPath, false))
                {
                    if (rgAnahtar != null)
                    {
                        return rgAnahtar.GetValue(RegistryKeyName) != null;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Windows başlangıç ayarı okunamadı: {ex.Message}");
            }
            return false;
        }
    }
}
