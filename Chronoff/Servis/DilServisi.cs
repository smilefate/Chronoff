using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Controls;
using ComboBoxItem = System.Windows.Controls.ComboBoxItem;

namespace Chronoff.Servis
{
    public class Diller
    {
        public string DosyaYolu { get; set; } = "";
        public string DilKodu { get; set; } = "";
        public string Ulke { get; set; } = "";
        public string DilAdi { get; set; } = "";

        public override string ToString() => DilAdi;
    }

    public static class DilServisi
    {
        public static Dictionary<string, string> AktifDil { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public static string AppDataDilKlasoru()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string folder = Path.Combine(appData, "Chronoff", "Languages");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            return folder;
        }

        public static void DilDosyalariniHazirla()
        {
            try
            {
                string hedefKlasor = AppDataDilKlasoru();
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                string resourcePrefix = "Chronoff.Languages.";

                string[] kaynaklar = assembly.GetManifestResourceNames();
                foreach (string kaynak in kaynaklar)
                {
                    if (kaynak.StartsWith(resourcePrefix) && kaynak.EndsWith(".ini", StringComparison.OrdinalIgnoreCase))
                    {
                        string dosyaAdi = kaynak.Substring(resourcePrefix.Length);
                        string hedefYol = Path.Combine(hedefKlasor, dosyaAdi);

                        if (!File.Exists(hedefYol) || new FileInfo(hedefYol).Length == 0)
                        {
                            using (Stream? stream = assembly.GetManifestResourceStream(kaynak))
                            {
                                if (stream != null)
                                {
                                    using (FileStream fileStream = new FileStream(hedefYol, FileMode.Create, FileAccess.Write))
                                    {
                                        stream.CopyTo(fileStream);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Dil dosyaları dışarı aktarılırken hata oluştu: {ex.Message}");
            }
        }

        /// <summary>TR: Belirtilen INI dosyasını okur ve sözlük olarak döner. EN: Parses the specified INI file and returns a dictionary.</summary>
        public static Dictionary<string, string> ParseIniFile(string dosyaYolu)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(dosyaYolu)) return dict;

            try
            {
                string dosyaAdi = Path.GetFileName(dosyaYolu);
                string hedefYol = dosyaYolu;

                if (!Path.IsPathRooted(hedefYol))
                {
                    hedefYol = Path.Combine(AppDataDilKlasoru(), dosyaAdi);
                    if (!File.Exists(hedefYol))
                    {
                        hedefYol = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Languages", dosyaAdi);
                    }
                }

                if (!File.Exists(hedefYol)) return dict;

                string mevcutBolum = "";
                foreach (var satir in File.ReadLines(hedefYol))
                {
                    string temizSatir = satir.Trim();
                    if (string.IsNullOrEmpty(temizSatir) || temizSatir.StartsWith(';') || temizSatir.StartsWith('#'))
                        continue;

                    if (temizSatir.StartsWith('[') && temizSatir.EndsWith(']'))
                    {
                        mevcutBolum = temizSatir[1..^1].Trim();
                        continue;
                    }

                    int esittirKonumu = temizSatir.IndexOf('=');
                    if (esittirKonumu > 0)
                    {
                        string anahtar = temizSatir[..esittirKonumu].Trim();
                        string deger = temizSatir[(esittirKonumu + 1)..].Trim();
                        if (deger.StartsWith('"') && deger.EndsWith('"') && deger.Length >= 2)
                        {
                            deger = deger[1..^1];
                        }
                        deger = deger.Replace("\\n", "\n");
                        string tamAnahtar = string.IsNullOrEmpty(mevcutBolum) ? anahtar : $"{mevcutBolum}.{anahtar}";
                        dict[tamAnahtar] = deger;
                    }
                }
            }
            catch { }

            return dict;
        }

        /// <summary>TR: Mevcut dil dosyalarını listeler. EN: Lists available language files.</summary>
        public static List<Diller> DilleriListele()
        {
            DilDosyalariniHazirla();

            string appDataDilKlasoru = AppDataDilKlasoru();
            string localDilKlasoru = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Languages");

            var list = new List<Diller>();
            var dosyaAdlari = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // AppData dil klasörünü tara
            if (Directory.Exists(appDataDilKlasoru))
            {
                try
                {
                    foreach (var dosya in Directory.GetFiles(appDataDilKlasoru, "*.ini"))
                    {
                        string dosyaAdi = Path.GetFileName(dosya);
                        if (dosyaAdlari.Contains(dosyaAdi)) continue;

                        var dilVerisi = ParseIniFile(dosya);
                        if (dilVerisi.TryGetValue("Language.langname", out var dilAdi))
                        {
                            var dilOgesi = new Diller
                            {
                                DosyaYolu = Path.GetFullPath(dosya),
                                DilKodu = dilVerisi.GetValueOrDefault("Language.langcode", "en"),
                                Ulke = dilVerisi.GetValueOrDefault("Language.country", "United States"),
                                DilAdi = dilAdi
                            };
                            list.Add(dilOgesi);
                            dosyaAdlari.Add(dosyaAdi);
                        }
                    }
                }
                catch { }
            }

            // Local dil klasörünü tara
            if (Directory.Exists(localDilKlasoru))
            {
                try
                {
                    foreach (var dosya in Directory.GetFiles(localDilKlasoru, "*.ini"))
                    {
                        string dosyaAdi = Path.GetFileName(dosya);
                        if (dosyaAdlari.Contains(dosyaAdi)) continue;

                        var dilVerisi = ParseIniFile(dosya);
                        if (dilVerisi.TryGetValue("Language.langname", out var dilAdi))
                        {
                            var dilOgesi = new Diller
                            {
                                DosyaYolu = Path.GetFullPath(dosya),
                                DilKodu = dilVerisi.GetValueOrDefault("Language.langcode", "en"),
                                Ulke = dilVerisi.GetValueOrDefault("Language.country", "United States"),
                                DilAdi = dilAdi
                            };
                            list.Add(dilOgesi);
                            dosyaAdlari.Add(dosyaAdi);
                        }
                    }
                }
                catch { }
            }

            return list;
        }

        /// <summary>TR: Mevcut dil dosyalarını asenkron listeler. EN: Lists available language files asynchronously.</summary>
        public static async Task<List<Diller>> DilleriListeleAsync()
        {
            return await Task.Run(() => DilleriListele());
        }

        public static void DilSozlugunuKaynaklaraEkle(Dictionary<string, string> dilPaketi)
        {
            if (dilPaketi == null || System.Windows.Application.Current == null) return;

            var resources = System.Windows.Application.Current.Resources;
            foreach (var cift in dilPaketi)
            {
                resources[cift.Key] = cift.Value;
            }
        }

        public static void Mesaj(
            string anahtarMetin,
            string varsayilanMetin,
            string baslikAnahtari = "Errors.Error",
            string varsayilanBaslik = "Hata",
            System.Windows.MessageBoxImage goruntu = System.Windows.MessageBoxImage.Warning,
            params object[] formatArgs)
        {
            string metin = AktifDil.GetValueOrDefault(anahtarMetin, varsayilanMetin);
            string baslik = AktifDil.GetValueOrDefault(baslikAnahtari, varsayilanBaslik);

            if (formatArgs.Length > 0)
            {
                try { metin = string.Format(metin, formatArgs); } catch { }
            }

            System.Windows.MessageBox.Show(metin, baslik, System.Windows.MessageBoxButton.OK, goruntu);
        }

        public static void ArayuzuYerellestir(Arayuz arayuz, Dictionary<string, string> dilPaketi)
        {
            DilSozlugunuKaynaklaraEkle(dilPaketi);

            string? metin;

            int h = ZamanBirimiYonetimi.SaatArtisiniAl(arayuz);
            int m = ZamanBirimiYonetimi.DakikaArtisiniAl(arayuz);
            if (dilPaketi.TryGetValue("MainScreen.AddHour", out metin))
            {
                string localizedH = string.Format(metin, h);
                if (arayuz.btnTarihSaatEkle != null) arayuz.btnTarihSaatEkle.Content = localizedH;
                if (arayuz.btnSaatEkle != null) arayuz.btnSaatEkle.Content = localizedH;
                if (arayuz.btnYinelemeSaatEkle != null) arayuz.btnYinelemeSaatEkle.Content = localizedH;
            }
            if (dilPaketi.TryGetValue("MainScreen.AddMinute", out metin))
            {
                string localizedM = string.Format(metin, m);
                if (arayuz.btnTarihSaatDakikaEkle != null) arayuz.btnTarihSaatDakikaEkle.Content = localizedM;
                if (arayuz.btnDakikaEkle != null) arayuz.btnDakikaEkle.Content = localizedM;
                if (arayuz.btnYinelemeDakikaEkle != null) arayuz.btnYinelemeDakikaEkle.Content = localizedM;
            }

            if (arayuz.YinelemeTipi != null)
            {
                int oldIdx = arayuz.YinelemeTipi.SelectedIndex;
                arayuz.YinelemeTipi.Items.Clear();
                arayuz.YinelemeTipi.Items.Add(dilPaketi.GetValueOrDefault("MainScreen.Daily", "Günlük"));
                arayuz.YinelemeTipi.Items.Add(dilPaketi.GetValueOrDefault("MainScreen.Weekly", "Haftalık"));
                arayuz.YinelemeTipi.Items.Add(dilPaketi.GetValueOrDefault("MainScreen.Monthly", "Aylık"));
                arayuz.YinelemeTipi.SelectedIndex = oldIdx >= 0 ? oldIdx : 0;
            }

            if (dilPaketi.TryGetValue("MainScreen.Reminder", out metin))
            {
                int braceIndex = metin.IndexOf("{0}");
                if (braceIndex >= 0)
                {
                    string prefix = metin[..braceIndex];
                    string suffix = metin[(braceIndex + 3)..];
                    if (arayuz.lblReminderLabel != null) arayuz.lblReminderLabel.Text = prefix;
                    if (arayuz.lblReminderSuffix != null) arayuz.lblReminderSuffix.Text = suffix;
                }
                else
                {
                    if (arayuz.lblReminderLabel != null) arayuz.lblReminderLabel.Text = metin;
                    if (arayuz.lblReminderSuffix != null) arayuz.lblReminderSuffix.Text = "";
                }
            }

            if (dilPaketi.TryGetValue("Days.Monday", out metin) && arayuz.chkPzt != null) arayuz.chkPzt.Content = metin[..Math.Min(3, metin.Length)];
            if (dilPaketi.TryGetValue("Days.Tuesday", out metin) && arayuz.chkSal != null) arayuz.chkSal.Content = metin[..Math.Min(3, metin.Length)];
            if (dilPaketi.TryGetValue("Days.Wednesday", out metin) && arayuz.chkCar != null) arayuz.chkCar.Content = metin[..Math.Min(3, metin.Length)];
            if (dilPaketi.TryGetValue("Days.Thursday", out metin) && arayuz.chkPer != null) arayuz.chkPer.Content = metin[..Math.Min(3, metin.Length)];
            if (dilPaketi.TryGetValue("Days.Friday", out metin) && arayuz.chkCum != null) arayuz.chkCum.Content = metin[..Math.Min(3, metin.Length)];
            if (dilPaketi.TryGetValue("Days.Saturday", out metin) && arayuz.chkCmt != null) arayuz.chkCmt.Content = metin[..Math.Min(3, metin.Length)];
            if (dilPaketi.TryGetValue("Days.Sunday", out metin) && arayuz.chkPaz != null) arayuz.chkPaz.Content = metin[..Math.Min(3, metin.Length)];

            if (arayuz.cmbDateFormat != null && arayuz.cmbDateFormat.Items.Count >= 2)
            {
                if (arayuz.cmbDateFormat.Items[0] is ComboBoxItem item0)
                    item0.Content = dilPaketi.GetValueOrDefault("Settings.DateFormat.DMY", "GG.AA.YYYY");
                if (arayuz.cmbDateFormat.Items[1] is ComboBoxItem item1)
                    item1.Content = dilPaketi.GetValueOrDefault("Settings.DateFormat.MDY", "AA.GG.YYYY");
            }
        }
    }
}