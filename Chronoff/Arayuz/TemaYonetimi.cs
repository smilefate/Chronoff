using System;
using System.Windows;
using System.Windows.Media;

namespace Chronoff
{
    public static class TemaYonetimi
    {
        /// <summary>TR: Koyu modun aktif olup olmadığını belirler. EN: Determines if dark mode is active.</summary>
        public static bool KoyuModMu(string tema)
        {
            if (tema == "sistem")
            {
                try
                {
                    return Microsoft.Win32.Registry.GetValue(
                        @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                        "AppsUseLightTheme", 1) as int? == 0;
                }
                catch
                {
                    return true;
                }
            }
            return tema == "koyu";
        }

        /// <summary>TR: Seçilen temayı arayüze uygular. EN: Applies the selected theme to the UI.</summary>
        public static void TemaUygula(ResourceDictionary resources, string tema)
        {
            bool koyuMod = KoyuModMu(tema);

            var koyuArkaplan = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(koyuMod ? "#1A1C1C" : "#F3F4F6");
            var yanPanelArkaplan = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(koyuMod ? "#1C1E1E" : "#E5E7EB");
            var yuzeyKapsayici = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(koyuMod ? "#2B2D2D" : "#FFFFFF");
            var dusukYuzey = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(koyuMod ? "#232525" : "#F9FAFB");
            var acikMetin = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(koyuMod ? "#F1F1F1" : "#1F2937");
            var solukMetin = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(koyuMod ? "#A0A6AE" : "#555E6B");
            var kenarlikRengi = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(koyuMod ? "#1AFFFFFF" : "#15000000");

            resources["BgDark"] = new SolidColorBrush(koyuArkaplan);
            resources["BgSidebar"] = new SolidColorBrush(yanPanelArkaplan);
            resources["SurfaceContainer"] = new SolidColorBrush(yuzeyKapsayici);
            resources["SurfaceLow"] = new SolidColorBrush(dusukYuzey);
            resources["TextLight"] = new SolidColorBrush(acikMetin);
            resources["TextMuted"] = new SolidColorBrush(solukMetin);
            resources["BorderColor"] = new SolidColorBrush(kenarlikRengi);
        }
    }
}
