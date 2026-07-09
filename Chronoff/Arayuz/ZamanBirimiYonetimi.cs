using System;
using System.Windows;
using System.Windows.Controls;
using Chronoff.Servis;
using TextBox = System.Windows.Controls.TextBox;
using Button = System.Windows.Controls.Button;
using ComboBox = System.Windows.Controls.ComboBox;
using RadioButton = System.Windows.Controls.RadioButton;

namespace Chronoff
{
    /// <summary>TR: Tarih güncelleme sonucunu taşır. EN: Holds the date update result.</summary>
    public record TarihGuncellemeSonucu(
        int Gun,
        int Ay,
        int Yil,
        int Saat,
        int Dakika,
        string? AmPm
    );

    /// <summary>TR: Sayaç ayarlama sonucunu taşır. EN: Holds the timer setting result.</summary>
    public record SayacAyarSonucu(
        int YeniSaat,
        int YeniDakika,
        string YeniAmPm,
        int GunDegisimi
    );

    public static class ZamanBirimiYonetimi
    {
        /// <summary>TR: Belirtilen tarihe gün ekler. EN: Adds days to the specified date.</summary>
        public static DateTime? TarihiHesapla(int gun, int ay, int yil, int gunSayisi)
        {
            try
            {
                DateTime dt = new DateTime(yil, ay, gun);
                return dt.AddDays(gunSayisi);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>TR: Geçmiş tarihi günceller. EN: Updates past date.</summary>
        public static TarihGuncellemeSonucu? GecmisTarihiGuncelle(
            int gun, int ay, int yil, int saat, int dakika, bool onIkiSaatMi, string amPm)
        {
            try
            {
                int h24 = saat;
                if (onIkiSaatMi)
                {
                    h24 = DurumPaneli.Convert12To24(saat, amPm);
                }

                DateTime girilenZaman = new DateTime(yil, ay, gun, h24, dakika, 0);
                if (girilenZaman < DateTime.Now)
                {
                    DateTime yarinkiZaman = DateTime.Now.Date.AddHours(h24).AddMinutes(dakika);
                    if (yarinkiZaman < DateTime.Now)
                    {
                        yarinkiZaman = yarinkiZaman.AddDays(1);
                    }

                    string? yeniAmPm = null;
                    int finalSaat = yarinkiZaman.Hour;
                    if (onIkiSaatMi)
                    {
                        var (h12, formattedAmPm) = DurumPaneli.Convert24To12(finalSaat);
                        finalSaat = h12;
                        yeniAmPm = formattedAmPm;
                    }

                    return new TarihGuncellemeSonucu(
                        yarinkiZaman.Day,
                        yarinkiZaman.Month,
                        yarinkiZaman.Year,
                        finalSaat,
                        yarinkiZaman.Minute,
                        yeniAmPm
                    );
                }
            }
            catch { }

            return null;
        }

        /// <summary>TR: Saat ve dakika değerlerini kutulardan alır. EN: Parses hour and minute values from text boxes.</summary>
        public static (int saat, int dakika) DegerleriAl(TextBox txtSaat, TextBox txtDakika)
        {
            int.TryParse(txtSaat?.Text, out int s);
            int.TryParse(txtDakika?.Text, out int d);
            return (s, d);
        }

        /// <summary>TR: Zaman metnini doğrular. EN: Validates time text.</summary>
        public static string ZamanMetniValideEt(string girdi, bool saatMi, bool is12Hour)
        {
            if (saatMi)
            {
                if (int.TryParse(girdi, out int saat))
                {
                    if (is12Hour)
                    {
                        if (saat > 12) saat = 12;
                        if (saat < 1)  saat = 12;
                    }
                    else
                    {
                        if (saat > 23) saat = 23;
                        if (saat < 0)  saat = 0;
                    }
                    return saat.ToString("D2");
                }
                else
                {
                    return is12Hour ? "12" : "00";
                }
            }
            else
            {
                if (int.TryParse(girdi, out int dakika))
                {
                    if (dakika > 59) dakika = 59;
                    return dakika.ToString("D2");
                }
                else
                {
                    return "00";
                }
            }
        }

        /// <summary>TR: Zaman kutusunun metnini doğrular. EN: Validates the text of a time text box.</summary>
        public static void ZamanValidasyonu(TextBox txt, bool saatMi, bool is12Hour)
        {
            if (txt == null) return;
            txt.Text = ZamanMetniValideEt(txt.Text, saatMi, is12Hour);
        }

        /// <summary>TR: Saat artış miktarını alır. EN: Gets the hour increment amount.</summary>
        public static int SaatArtisiniAl(Arayuz arayuz)
        {
            if (arayuz.txtSaatArtis != null && int.TryParse(arayuz.txtSaatArtis.Text, out int h) && h > 0)
                return h;
            return 1;
        }

        /// <summary>TR: Dakika artış miktarını alır. EN: Gets the minute increment amount.</summary>
        public static int DakikaArtisiniAl(Arayuz arayuz)
        {
            if (arayuz.txtDakikaArtis != null && int.TryParse(arayuz.txtDakikaArtis.Text, out int m) && m > 0)
                return m;
            return 5;
        }

        /// <summary>TR: Azaltma eyleminin yapılıp yapılmayacağını belirler. EN: Determines if decrement action should run.</summary>
        public static bool AzaltmaYapilacakMi(Arayuz arayuz, object? sender)
        {
            if (arayuz.chkButonTersEylem?.IsChecked != true) return false;
            return System.Windows.Input.Mouse.RightButton == System.Windows.Input.MouseButtonState.Pressed;
        }

        /// <summary>TR: Zamanlayıcı ayarlarını hesaplar. EN: Calculates timer settings.</summary>
        public static SayacAyarSonucu SayacAyarHesapla(
            string tag,
            bool azaltmaModu,
            int saatArtisi,
            int dakikaArtisi,
            int aktifSekmeIndex,
            int mevcutSaat,
            int mevcutDakika,
            bool onIkiSaatModu,
            string amPm,
            int gun,
            int ay,
            int yil)
        {
            bool saatMi = tag.Contains("Saat");
            int yeniSaat = mevcutSaat;
            int yeniDakika = mevcutDakika;
            string yeniAmPm = amPm;
            int gunDegisimi = 0;

            if (saatMi)
            {
                int mevcutSaat24 = mevcutSaat;
                if (onIkiSaatModu)
                {
                    mevcutSaat24 = DurumPaneli.Convert12To24(mevcutSaat, amPm);
                }

                if (azaltmaModu)
                {
                    if (aktifSekmeIndex == 0 && mevcutSaat24 - saatArtisi < 0)
                    {
                        try
                        {
                            DateTime mevcutTarih = new DateTime(yil, ay, gun);
                            if (mevcutTarih.AddDays(-1) >= DateTime.Today)
                            {
                                gunDegisimi = -1;
                            }
                        }
                        catch { }
                    }
                    mevcutSaat24 = (mevcutSaat24 - saatArtisi < 0)
                        ? 24 + ((mevcutSaat24 - saatArtisi) % 24)
                        : mevcutSaat24 - saatArtisi;
                    if (mevcutSaat24 < 0) mevcutSaat24 = 0;
                }
                else
                {
                    if (aktifSekmeIndex == 0 && mevcutSaat24 + saatArtisi >= 24)
                    {
                        gunDegisimi = 1;
                    }
                    mevcutSaat24 = (mevcutSaat24 + saatArtisi) % 24;
                }

                if (onIkiSaatModu)
                {
                    var (h12, newAmPm) = DurumPaneli.Convert24To12(mevcutSaat24);
                    yeniSaat = h12;
                    yeniAmPm = newAmPm;
                }
                else
                {
                    yeniSaat = mevcutSaat24;
                }
            }
            else
            {
                if (azaltmaModu)
                {
                    yeniDakika = (mevcutDakika - dakikaArtisi < 0)
                        ? 60 + ((mevcutDakika - dakikaArtisi) % 60)
                        : mevcutDakika - dakikaArtisi;
                    if (yeniDakika < 0) yeniDakika = 0;
                }
                else
                {
                    yeniDakika = (mevcutDakika + dakikaArtisi) % 60;
                }
            }

            return new SayacAyarSonucu(yeniSaat, yeniDakika, yeniAmPm, gunDegisimi);
        }
    }
}