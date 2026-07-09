using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using Chronoff.Core;

namespace Chronoff.Servis
{
    public class ChronoffAyarlar
    {
        // --- Genel Ayarlar ---
        public string Theme { get; set; } = "koyu";
        public int SelectedTab { get; set; } = 1;
        public string SelectedTask { get; set; } = "kapat";
        public string LanguageFilePath { get; set; } = "";
        public string DateFormat { get; set; } = "GG.AA.YYYY";
        public string AccentColor { get; set; } = "#0078D4";

        // --- Güvenlik Ayarları ---
        public bool PasswordProtectionEnabled { get; set; } = false;
        public string ProtectionPassword { get; set; } = "";

        // --- Davranış Ayarları ---
        public bool RunAtStartup { get; set; } = false;
        public bool RunInBackground { get; set; } = true;

        // --- Sayaç Ayarları ---
        public string CountdownHourText { get; set; } = "00";
        public string CountdownMinuteText { get; set; } = "30";
        public bool Format12Hour { get; set; } = false;
        public string DateTimeAmPm { get; set; } = "ÖÖ";

        // --- Yineleme Ayarları ---
        public bool RecurrenceActive { get; set; } = false;
        public int RecurrenceType { get; set; } = 0;
        public string RecurrenceHourText { get; set; } = "00";
        public string RecurrenceMinuteText { get; set; } = "00";
        public string RecurrenceAmPm { get; set; } = "ÖÖ";
        public bool Monday { get; set; } = true;
        public bool Tuesday { get; set; } = false;
        public bool Wednesday { get; set; } = false;
        public bool Thursday { get; set; } = false;
        public bool Friday { get; set; } = true;
        public bool Saturday { get; set; } = false;
        public bool Sunday { get; set; } = false;
        public List<int> SelectedMonthDays { get; set; } = new List<int>();

        // --- Arayüz / Süre Butonları Ayarları ---
        public bool ShowTimeIncrement { get; set; } = true;
        public bool ShowDateTime { get; set; } = false;
        public bool ShowCountdown { get; set; } = true;
        public bool ShowRecurrence { get; set; } = false;
        public bool UseReverseBehavior { get; set; } = true;
        public int HourIncrementAmount { get; set; } = 1;
        public int MinuteIncrementAmount { get; set; } = 5;

        // --- Hatırlatıcı Ayarları ---
        public bool ShowReminder { get; set; } = true;
        public string ReminderMinutes { get; set; } = "15";
        public bool AlwaysEnableReminder { get; set; } = false;
        public int DefaultReminderMinutes { get; set; } = 15;
        public List<ErtelemeSecenegiOgesi> SnoozeOptions { get; set; } = new List<ErtelemeSecenegiOgesi>();
    }

    public class ErtelemeSecenegiOgesi
    {
        public int Value { get; set; } = 5;
        public string Unit { get; set; } = "Dakika";
    }

    public static class AyarServisi
    {
        private static readonly SemaphoreSlim _ayarKilidi = new SemaphoreSlim(1, 1);

        public static string SistemGorevidenString(SistemGorevi gorev) => gorev switch
        {
            SistemGorevi.YenidenBaslat => "yenidenbaslat",
            SistemGorevi.Sleep => "sleep",
            SistemGorevi.Hibernate => "hibernate",
            SistemGorevi.Kilitle => "kilitle",
            SistemGorevi.OturumuKapat => "oturumukapat",
            _ => "kapat"
        };

        public static SistemGorevi StringdenSistemGorevi(string? deger) => deger switch
        {
            "yenidenbaslat" => SistemGorevi.YenidenBaslat,
            "sleep" => SistemGorevi.Sleep,
            "hibernate" => SistemGorevi.Hibernate,
            "kilitle" => SistemGorevi.Kilitle,
            "oturumukapat" => SistemGorevi.OturumuKapat,
            _ => SistemGorevi.Kapat
        };

        public static string AyarDosyaKonumu()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string folder = Path.Combine(appData, "Chronoff");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            return Path.Combine(folder, "settings.json");
        }

        public static async System.Threading.Tasks.Task AyarlariKaydetAsync(ChronoffAyarlar ayarlar)
        {
            await _ayarKilidi.WaitAsync();
            try
            {
                string ayarJson = JsonSerializer.Serialize(ayarlar, ChronoffJsonContext.Default.ChronoffAyarlar);
                await File.WriteAllTextAsync(AyarDosyaKonumu(), ayarJson);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ayarlar kaydedilirken hata oluştu: {ex.Message}");
            }
            finally
            {
                _ayarKilidi.Release();
            }
        }

        public static async System.Threading.Tasks.Task<ChronoffAyarlar?> AyarlariOkuAsync()
        {
            try
            {
                string ayarDosyasi = AyarDosyaKonumu();
                if (File.Exists(ayarDosyasi))
                {
                    string ayarJson = await File.ReadAllTextAsync(ayarDosyasi);
                    return JsonSerializer.Deserialize(ayarJson, ChronoffJsonContext.Default.ChronoffAyarlar);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ayarlar okunurken hata oluştu: {ex.Message}");
            }
            return null;
        }

        public static ChronoffAyarlar? AyarlariOku()
        {
            try
            {
                string ayarDosyasi = AyarDosyaKonumu();
                if (File.Exists(ayarDosyasi))
                {
                    string ayarJson = File.ReadAllText(ayarDosyasi);
                    return JsonSerializer.Deserialize(ayarJson, ChronoffJsonContext.Default.ChronoffAyarlar);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ayarlar okunurken hata oluştu: {ex.Message}");
            }
            return null;
        }
    }

    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(ChronoffAyarlar))]
    internal partial class ChronoffJsonContext : JsonSerializerContext
    {
    }

}
