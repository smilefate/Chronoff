using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Chronoff.Core;
using Chronoff.Servis;

namespace Chronoff
{
    /// <summary>TR: Durum paneli ve yerelleştirme metinlerini yönetir. EN: Manages status panel and localization text.</summary>
    public static class DurumPaneli
    {
        private static bool _sonSayacAktif;
        private static int _sonAktifSekmeIndex = -1;
        private static string _sonSistemEylemi = "";
        private static int _sonKalansure = -1;
        private static int _sonUyariDakikasi = -1;
        private static bool _sonHatirlaticiAktif;
        private static bool? _sonHatirlaticiHerZaman;
        private static bool? _sonOnIkiSaat;
        private static object? _sonDil;

        private static int _sonKalanSaat = -1;
        private static int _sonKalanDakika = -1;

        private static string _sonGunMetni = "";
        private static string _sonAyMetni = "";
        private static string _sonYilMetni = "";
        private static string _sonSaatMetni = "";
        private static string _sonDakikaMetni = "";
        private static string _sonTarihSaatAmPm = "";

        private static string _sonSaatGir = "";
        private static string _sonDakikaGir = "";

        private static string _sonYinelemeSaat = "";
        private static string _sonYinelemeDakika = "";
        private static int _sonYinelemeTipi = -1;
        private static string _sonYinelemeAmPm = "";
        private static bool _sonPzt, _sonSal, _sonCar, _sonPer, _sonCum, _sonCmt, _sonPaz;
        private static int _sonAyinGunleriHash = -1;

        /// <summary>TR: 12 saat formatını 24 saat formatına çevirir. EN: Converts 12-hour format to 24-hour format.</summary>
        public static int Convert12To24(int saat12, string saatAmPm)
        {
            string saatPM = DilServisi.AktifDil?.GetValueOrDefault("Time.Pm", "ÖS") ?? "ÖS";
            if (string.Equals(saatAmPm, saatPM, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(saatAmPm, "ÖS", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(saatAmPm, "PM", StringComparison.OrdinalIgnoreCase))
            {
                return saat12 == 12 ? 12 : saat12 + 12;
            }
            else
            {
                return saat12 == 12 ? 0 : saat12;
            }
        }

        /// <summary>TR: 24 saat formatını 12 saat formatına çevirir. EN: Converts 24-hour format to 12-hour format.</summary>
        public static (int saat12, string saatAmPm) Convert24To12(int saat24)
        {
            string saatAM = DilServisi.AktifDil?.GetValueOrDefault("Time.Am", "ÖÖ") ?? "ÖÖ";
            string saatPM = DilServisi.AktifDil?.GetValueOrDefault("Time.Pm", "ÖS") ?? "ÖS";
            string saatAmPm = saat24 >= 12 ? saatPM : saatAM;
            int saat12 = saat24 % 12;
            if (saat12 == 0) saat12 = 12;
            return (saat12, saatAmPm);
        }

        /// <summary>TR: Saati belirtilen formatta biçimlendirir. EN: Formats the time with the specified pattern.</summary>
        public static string SaatFormati(int saatler, int dakikalar, bool onIkiSaatKullan)
        {
            if (onIkiSaatKullan)
            {
                string saatAM = "ÖÖ";
                string saatPM = "ÖS";
                if (DilServisi.AktifDil != null)
                {
                    if (DilServisi.AktifDil.TryGetValue("Time.Am", out var a)) saatAM = a;
                    if (DilServisi.AktifDil.TryGetValue("Time.Pm", out var p)) saatPM = p;
                }
                string saatAmPm = saatler >= 12 ? saatPM : saatAM;
                int saat12 = saatler % 12;
                if (saat12 == 0) saat12 = 12;
                return $"{saat12:D2}:{dakikalar:D2} {saatAmPm}";
            }
            return $"{saatler:D2}:{dakikalar:D2}";
        }

        private static void MetniDegistir(TextBlock? metinBlogu, string deger)
        {
            if (metinBlogu != null && metinBlogu.Text != deger)
            {
                metinBlogu.Text = deger;
            }
        }

        /// <summary>TR: Saat etiketlerini günceller. EN: Updates time labels.</summary>
        public static void SaatEtiketiniGuncelle(Arayuz arayuz)
        {
            if (arayuz.btnTarihSaatAmPm == null || arayuz.btnYinelemeAmPm == null) return;

            bool saat12Format = arayuz.chkOnIkiSaat?.IsChecked is true;
            if (!saat12Format)
            {
                if (arayuz.btnTarihSaatAmPm.Visibility != Visibility.Collapsed)
                    arayuz.btnTarihSaatAmPm.Visibility = Visibility.Collapsed;
                if (arayuz.btnYinelemeAmPm.Visibility != Visibility.Collapsed)
                    arayuz.btnYinelemeAmPm.Visibility = Visibility.Collapsed;
                return;
            }

            if (arayuz.btnTarihSaatAmPm.Visibility != Visibility.Visible)
                arayuz.btnTarihSaatAmPm.Visibility = Visibility.Visible;
            if (arayuz.btnYinelemeAmPm.Visibility != Visibility.Visible)
                arayuz.btnYinelemeAmPm.Visibility = Visibility.Visible;

            string saatAM = DilServisi.AktifDil?.GetValueOrDefault("Time.Am", "ÖÖ") ?? "ÖÖ";
            string saatPM = DilServisi.AktifDil?.GetValueOrDefault("Time.Pm", "ÖS") ?? "ÖS";

            string mevcutTarihAmPm = arayuz.btnTarihSaatAmPm.Content?.ToString() ?? "";
            string hedefTarihAmPm = (mevcutTarihAmPm == "ÖS" || mevcutTarihAmPm == "PM" || mevcutTarihAmPm == saatPM) ? saatPM : saatAM;
            if (mevcutTarihAmPm != hedefTarihAmPm)
                arayuz.btnTarihSaatAmPm.Content = hedefTarihAmPm;

            string mevcutYinelemeAmPm = arayuz.btnYinelemeAmPm.Content?.ToString() ?? "";
            string hedefYinelemeAmPm = (mevcutYinelemeAmPm == "ÖS" || mevcutYinelemeAmPm == "PM" || mevcutYinelemeAmPm == saatPM) ? saatPM : saatAM;
            if (mevcutYinelemeAmPm != hedefYinelemeAmPm)
                arayuz.btnYinelemeAmPm.Content = hedefYinelemeAmPm;
        }

        /// <summary>TR: Hatırlatıcı çubuğu görünürlüğünü ayarlar. EN: Configures reminder bar visibility.</summary>
        public static void HatirlaticiGorunumu(Arayuz arayuz)
        {
            if (arayuz.ReminderBarBorder != null)
            {
                var hedefGorunurluk = (arayuz.chkHatirlaticiGoster?.IsChecked is true && arayuz.chkHatirlaticiHerZaman?.IsChecked is not true)
                    ? Visibility.Visible
                    : Visibility.Collapsed;

                if (arayuz.ReminderBarBorder.Visibility != hedefGorunurluk)
                {
                    arayuz.ReminderBarBorder.Visibility = hedefGorunurluk;
                }
            }
        }

        /// <summary>TR: Durum panelini günceller. EN: Updates the status panel.</summary>
        public static void Guncelle(Arayuz arayuz)
        {
            if (arayuz.DurumMetni == null || arayuz.DurumHatirlaticiMetni == null || arayuz.TarihGunGir == null || arayuz.TarihAyGir == null || arayuz.TarihYilGir == null ||
                arayuz.TarihSaatGir == null || arayuz.TarihDakikaGir == null || arayuz.SaatGir == null || arayuz.DakikaGir == null ||
                arayuz.YinelemeTipi == null || arayuz.chkPzt == null || arayuz.chkSal == null || arayuz.chkCar == null || arayuz.chkPer == null ||
                arayuz.chkCum == null || arayuz.chkCmt == null || arayuz.chkPaz == null || arayuz.AyinGunleriList == null ||
                arayuz.YinelemeSaat == null || arayuz.YinelemeDakika == null)
            {
                return;
            }

            bool sayacAktif = SayacServisi.Instance.IsEnabled;
            int aktifSekmeIndex = arayuz._aktifSekmeIndex;
            string sistemEylemi = arayuz.SistemEylemiAdiniAl();
            int kalansure = arayuz._kalansure;
            int uyariDakikasi = arayuz._uyariDakikasi;
            bool hatirlaticiAktif = arayuz._hatirlaticiAktif;

            if (!sayacAktif)
            {
                if (arayuz.chkHatirlaticiHerZaman?.IsChecked == true)
                {
                    hatirlaticiAktif = true;
                    uyariDakikasi = int.TryParse(arayuz.txtHatirlatmaDk?.Text, out int defDk) ? defDk : 15;
                }
                else
                {
                    hatirlaticiAktif = arayuz.HatirlaticiToggle?.IsChecked == true;
                    uyariDakikasi = int.TryParse(arayuz.PopupDkGir?.Text, out int popDk) ? popDk : 15;
                }
            }
            bool? hatirlaticiHerZaman = arayuz.chkHatirlaticiHerZaman?.IsChecked;
            bool? onIkiSaat = arayuz.chkOnIkiSaat?.IsChecked;
            object? aktifDil = DilServisi.AktifDil;

            string gunMetni = "";
            string ayMetni = "";
            string yilMetni = "";
            string saatMetni = "";
            string dakikaMetni = "";
            string tarihSaatAmPm = "";

            string saatGir = "";
            string dakikaGir = "";

            string yinelemeSaat = "";
            string yinelemeDakika = "";
            int yinelemeTipi = 0;
            string yinelemeAmPm = "";
            bool pzt = false, sal = false, car = false, per = false, cum = false, cmt = false, paz = false;
            int ayinGunleriHash = 0;

            if (aktifSekmeIndex == 0)
            {
                gunMetni = arayuz.TarihGunGir.SelectedItem?.ToString() ?? arayuz.TarihGunGir.Text ?? "01";
                ayMetni = arayuz.TarihAyGir.SelectedItem?.ToString() ?? arayuz.TarihAyGir.Text ?? "01";
                yilMetni = arayuz.TarihYilGir.SelectedItem?.ToString() ?? arayuz.TarihYilGir.Text ?? DateTime.Now.Year.ToString();
                saatMetni = arayuz.TarihSaatGir.Text;
                dakikaMetni = arayuz.TarihDakikaGir.Text;
                tarihSaatAmPm = arayuz.btnTarihSaatAmPm?.Content?.ToString() ?? "ÖÖ";
            }
            else if (aktifSekmeIndex == 1)
            {
                saatGir = arayuz.SaatGir.Text;
                dakikaGir = arayuz.DakikaGir.Text;
            }
            else if (aktifSekmeIndex == 2)
            {
                yinelemeSaat = arayuz.YinelemeSaat.Text;
                yinelemeDakika = arayuz.YinelemeDakika.Text;
                yinelemeTipi = arayuz.YinelemeTipi.SelectedIndex;
                yinelemeAmPm = arayuz.btnYinelemeAmPm?.Content?.ToString() ?? "ÖÖ";
                pzt = arayuz.chkPzt.IsChecked == true;
                sal = arayuz.chkSal.IsChecked == true;
                car = arayuz.chkCar.IsChecked == true;
                per = arayuz.chkPer.IsChecked == true;
                cum = arayuz.chkCum.IsChecked == true;
                cmt = arayuz.chkCmt.IsChecked == true;
                paz = arayuz.chkPaz.IsChecked == true;

                int ozet = 17;
                foreach (var oge in arayuz.AyinGunleriList.SelectedItems)
                {
                    if (oge != null)
                    {
                        ozet = ozet * 31 + oge.GetHashCode();
                    }
                }
                ayinGunleriHash = ozet;
            }

            int kalanSaat = 0;
            int kalanDakika = 0;
            if (sayacAktif && aktifSekmeIndex == 1)
            {
                kalanSaat = kalansure / 3600;
                kalanDakika = (kalansure % 3600) / 60;
            }

            bool degisti =
                sayacAktif != _sonSayacAktif ||
                aktifSekmeIndex != _sonAktifSekmeIndex ||
                sistemEylemi != _sonSistemEylemi ||
                kalansure != _sonKalansure ||
                uyariDakikasi != _sonUyariDakikasi ||
                hatirlaticiAktif != _sonHatirlaticiAktif ||
                hatirlaticiHerZaman != _sonHatirlaticiHerZaman ||
                onIkiSaat != _sonOnIkiSaat ||
                aktifDil != _sonDil ||
                kalanSaat != _sonKalanSaat ||
                kalanDakika != _sonKalanDakika;

            if (!degisti)
            {
                if (aktifSekmeIndex == 0)
                {
                    degisti =
                        gunMetni != _sonGunMetni ||
                        ayMetni != _sonAyMetni ||
                        yilMetni != _sonYilMetni ||
                        saatMetni != _sonSaatMetni ||
                        dakikaMetni != _sonDakikaMetni ||
                        tarihSaatAmPm != _sonTarihSaatAmPm;
                }
                else if (aktifSekmeIndex == 1)
                {
                    degisti =
                        saatGir != _sonSaatGir ||
                        dakikaGir != _sonDakikaGir;
                }
                else if (aktifSekmeIndex == 2)
                {
                    degisti =
                        yinelemeSaat != _sonYinelemeSaat ||
                        yinelemeDakika != _sonYinelemeDakika ||
                        yinelemeTipi != _sonYinelemeTipi ||
                        yinelemeAmPm != _sonYinelemeAmPm ||
                        pzt != _sonPzt ||
                        sal != _sonSal ||
                        car != _sonCar ||
                        per != _sonPer ||
                        cum != _sonCum ||
                        cmt != _sonCmt ||
                        paz != _sonPaz ||
                        ayinGunleriHash != _sonAyinGunleriHash;
                }
            }

            if (!degisti)
            {
                return;
            }

            // Cache values
            _sonSayacAktif = sayacAktif;
            _sonAktifSekmeIndex = aktifSekmeIndex;
            _sonSistemEylemi = sistemEylemi;
            _sonKalansure = kalansure;
            _sonUyariDakikasi = uyariDakikasi;
            _sonHatirlaticiAktif = hatirlaticiAktif;
            _sonHatirlaticiHerZaman = hatirlaticiHerZaman;
            _sonOnIkiSaat = onIkiSaat;
            _sonDil = aktifDil;
            _sonKalanSaat = kalanSaat;
            _sonKalanDakika = kalanDakika;

            if (aktifSekmeIndex == 0)
            {
                _sonGunMetni = gunMetni;
                _sonAyMetni = ayMetni;
                _sonYilMetni = yilMetni;
                _sonSaatMetni = saatMetni;
                _sonDakikaMetni = dakikaMetni;
                _sonTarihSaatAmPm = tarihSaatAmPm;
            }
            else if (aktifSekmeIndex == 1)
            {
                _sonSaatGir = saatGir;
                _sonDakikaGir = dakikaGir;
            }
            else if (aktifSekmeIndex == 2)
            {
                _sonYinelemeSaat = yinelemeSaat;
                _sonYinelemeDakika = yinelemeDakika;
                _sonYinelemeTipi = yinelemeTipi;
                _sonYinelemeAmPm = yinelemeAmPm;
                _sonPzt = pzt;
                _sonSal = sal;
                _sonCar = car;
                _sonPer = per;
                _sonCum = cum;
                _sonCmt = cmt;
                _sonPaz = paz;
                _sonAyinGunleriHash = ayinGunleriHash;
            }

            SaatEtiketiniGuncelle(arayuz);
            HatirlaticiGorunumu(arayuz);

            bool hedefEtkinlik = !sayacAktif;

            if (arayuz.rbTabTarihSaat != null && arayuz.rbTabTarihSaat.IsEnabled != hedefEtkinlik) arayuz.rbTabTarihSaat.IsEnabled = hedefEtkinlik;
            if (arayuz.rbTabGeriSayim != null && arayuz.rbTabGeriSayim.IsEnabled != hedefEtkinlik) arayuz.rbTabGeriSayim.IsEnabled = hedefEtkinlik;
            if (arayuz.rbTabYineleme != null && arayuz.rbTabYineleme.IsEnabled != hedefEtkinlik) arayuz.rbTabYineleme.IsEnabled = hedefEtkinlik;
            if (arayuz.rbNavZamanlayicilar != null && arayuz.rbNavZamanlayicilar.IsEnabled != hedefEtkinlik) arayuz.rbNavZamanlayicilar.IsEnabled = hedefEtkinlik;
            if (arayuz.rbNavAyarlar != null && arayuz.rbNavAyarlar.IsEnabled != hedefEtkinlik) arayuz.rbNavAyarlar.IsEnabled = hedefEtkinlik;
            if (arayuz.rbNavGorunum != null && arayuz.rbNavGorunum.IsEnabled != hedefEtkinlik) arayuz.rbNavGorunum.IsEnabled = hedefEtkinlik;
            if (arayuz.rbNavHakkinda != null && arayuz.rbNavHakkinda.IsEnabled != hedefEtkinlik) arayuz.rbNavHakkinda.IsEnabled = hedefEtkinlik;

            string saatBirimi = DilServisi.AktifDil.GetValueOrDefault("Time.Hour", "saat");
            string dakikaBirimi = DilServisi.AktifDil.GetValueOrDefault("Time.Minute", "dk");

            if (sayacAktif && arayuz._aktifSekmeIndex == 1)
            {
                int saat = kalanSaat;
                int dakika = kalanDakika;

                string kalanSureMetni = DilServisi.AktifDil.GetValueOrDefault("Status.RemainingTime", "{0} eylemine kalan süre: {1}");
                string zamanMetni = "";
                if (saat > 0 && dakika > 0)
                {
                    zamanMetni = $"{saat} {saatBirimi} {dakika} {dakikaBirimi}";
                }
                else if (saat > 0 && dakika == 0)
                {
                    zamanMetni = $"{saat} {saatBirimi}";
                }
                else
                {
                    zamanMetni = $"{dakika} {dakikaBirimi}";
                }
                MetniDegistir(arayuz.DurumMetni, string.Format(kalanSureMetni, sistemEylemi, zamanMetni));
            }
            else if (arayuz._aktifSekmeIndex == 0)
            {
                if (string.IsNullOrEmpty(saatMetni)) saatMetni = "00";
                if (string.IsNullOrEmpty(dakikaMetni)) dakikaMetni = "00";

                if (int.TryParse(gunMetni, out int gunSayisi)) gunMetni = gunSayisi.ToString("D2");
                if (int.TryParse(ayMetni, out int aySayisi)) ayMetni = aySayisi.ToString("D2");
                if (int.TryParse(saatMetni, out int saatSayisi)) saatMetni = saatSayisi.ToString("D2");
                if (int.TryParse(dakikaMetni, out int dakikaSayisi)) dakikaMetni = dakikaSayisi.ToString("D2");

                string tarih = (arayuz._tarihFormati == "AA.GG.YYYY") ? $"{ayMetni}.{gunMetni}.{yilMetni}" : $"{gunMetni}.{ayMetni}.{yilMetni}";
                int.TryParse(saatMetni, out int h);
                int.TryParse(dakikaMetni, out int m);

                if (arayuz.chkOnIkiSaat?.IsChecked == true)
                {
                    string amPm = tarihSaatAmPm;
                    h = Convert12To24(h, amPm);
                }

                string saat = SaatFormati(h, m, arayuz.chkOnIkiSaat?.IsChecked == true);
                string planlananZamanMetni = DilServisi.AktifDil.GetValueOrDefault("Status.ActionScheduled", "{0} eylemi için planlanan zaman:\n{1} - {2}");
                MetniDegistir(arayuz.DurumMetni, string.Format(planlananZamanMetni, sistemEylemi, tarih, saat));
            }
            else if (arayuz._aktifSekmeIndex == 1)
            {
                int.TryParse(saatGir, out int saat);
                int.TryParse(dakikaGir, out int dakika);

                string kalanSureMetni = DilServisi.AktifDil.GetValueOrDefault("Status.RemainingTime", "{0} eylemine kalan süre: {1}");
                string zamanMetni = "";
                if (saat > 0 && dakika > 0)
                {
                    zamanMetni = $"{saat} {saatBirimi} {dakika} {dakikaBirimi}";
                }
                else if (saat > 0 && dakika == 0)
                {
                    zamanMetni = $"{saat} {saatBirimi}";
                }
                else
                {
                    zamanMetni = $"{dakika} {dakikaBirimi}";
                }
                MetniDegistir(arayuz.DurumMetni, string.Format(kalanSureMetni, sistemEylemi, zamanMetni));
            }
            else if (arayuz._aktifSekmeIndex == 2)
            {
                if (string.IsNullOrEmpty(yinelemeSaat)) yinelemeSaat = "00";
                if (string.IsNullOrEmpty(yinelemeDakika)) yinelemeDakika = "00";
                if (int.TryParse(yinelemeSaat, out int saatSayisi)) yinelemeSaat = saatSayisi.ToString("D2");
                if (int.TryParse(yinelemeDakika, out int dakikaSayisi)) yinelemeDakika = dakikaSayisi.ToString("D2");
                int.TryParse(yinelemeSaat, out int h);
                int.TryParse(yinelemeDakika, out int m);

                if (arayuz.chkOnIkiSaat?.IsChecked == true)
                {
                    string amPm = yinelemeAmPm;
                    h = Convert12To24(h, amPm);
                }

                string saat = SaatFormati(h, m, arayuz.chkOnIkiSaat?.IsChecked == true);

                var recurrence = (RecurrenceType)yinelemeTipi;
                if (recurrence == RecurrenceType.Daily)
                {
                    string gunlukMetin = DilServisi.AktifDil.GetValueOrDefault("Status.DailyScheduled", "{0} eylemi her gün tekrarlanacak.\nSaat: {1}");
                    MetniDegistir(arayuz.DurumMetni, string.Format(gunlukMetin, sistemEylemi, saat));
                }
                else if (recurrence == RecurrenceType.Weekly)
                {
                    bool tumGunlerSecili = pzt && sal && car && per && cum && cmt && paz;
                    if (tumGunlerSecili)
                    {
                        string gunlukMetin = DilServisi.AktifDil.GetValueOrDefault("Status.DailyScheduled", "{0} eylemi her gün tekrarlanacak.\nSaat: {1}");
                        MetniDegistir(arayuz.DurumMetni, string.Format(gunlukMetin, sistemEylemi, saat));
                    }
                    else
                    {
                        List<string> gunler = new List<string>();
                        if (pzt) gunler.Add(DilServisi.AktifDil.GetValueOrDefault("Days.Monday", "Pazartesi"));
                        if (sal) gunler.Add(DilServisi.AktifDil.GetValueOrDefault("Days.Tuesday", "Salı"));
                        if (car) gunler.Add(DilServisi.AktifDil.GetValueOrDefault("Days.Wednesday", "Çarşamba"));
                        if (per) gunler.Add(DilServisi.AktifDil.GetValueOrDefault("Days.Thursday", "Perşembe"));
                        if (cum) gunler.Add(DilServisi.AktifDil.GetValueOrDefault("Days.Friday", "Cuma"));
                        if (cmt) gunler.Add(DilServisi.AktifDil.GetValueOrDefault("Days.Saturday", "Cumartesi"));
                        if (paz) gunler.Add(DilServisi.AktifDil.GetValueOrDefault("Days.Sunday", "Pazar"));

                        string gunMetniList = gunler.Count > 0 ? string.Join(", ", gunler) : "seçilen";
                        string haftalikMetin = DilServisi.AktifDil.GetValueOrDefault("Status.WeeklyScheduled", "{0} eylemi her {1} tekrarlanacak.\nSaat: {2}");
                        MetniDegistir(arayuz.DurumMetni, string.Format(haftalikMetin, sistemEylemi, gunMetniList, saat));
                    }
                }
                else if (recurrence == RecurrenceType.Monthly)
                {
                    var secilenGunler = arayuz.AyinGunleriList.SelectedItems
                        .Cast<ListBoxItem>()
                        .Where(oge => int.TryParse(oge.Content.ToString(), out _))
                        .Select(oge => int.Parse(oge.Content.ToString()!))
                        .OrderBy(sayi => sayi)
                        .ToList();

                    if (secilenGunler.Count > 3 || secilenGunler.Count == 0)
                    {
                        string aylikMetin = DilServisi.AktifDil.GetValueOrDefault("Status.MonthlyScheduled", "{0} eylemi her ayın seçilen günlerinde tekrarlanacak.\nSaat: {1}");
                        MetniDegistir(arayuz.DurumMetni, string.Format(aylikMetin, sistemEylemi, saat));
                    }
                    else
                    {
                        string secilenGunMetni = string.Join(", ", secilenGunler);
                        string ozelAylikMetin = DilServisi.AktifDil.GetValueOrDefault("Status.MonthlyScheduledSpecific", "{0} eylemi her ayın {1}. günlerinde tekrarlanacak.\nSaat: {2}");
                        MetniDegistir(arayuz.DurumMetni, string.Format(ozelAylikMetin, sistemEylemi, secilenGunMetni, saat));
                    }
                }
            }

            if (!hatirlaticiAktif)
            {
                MetniDegistir(arayuz.DurumHatirlaticiMetni, DilServisi.AktifDil.GetValueOrDefault("Status.ReminderOff", "Hatırlatıcı Kapalı"));
            }
            else if (hatirlaticiHerZaman == true && !sayacAktif)
            {
                MetniDegistir(arayuz.DurumHatirlaticiMetni, DilServisi.AktifDil.GetValueOrDefault("Status.ReminderAlwaysOn", "Hatırlatıcı Her Zaman Etkin"));
            }
            else if (sayacAktif)
            {
                int farkSaniye = kalansure - (uyariDakikasi * 60);
                if (farkSaniye > 0)
                {
                    string planlananHatirlaticiMetni = DilServisi.AktifDil.GetValueOrDefault("Status.ReminderScheduled", "Hatırlatıcı {0} dakika kala gösterilecek");
                    MetniDegistir(arayuz.DurumHatirlaticiMetni, string.Format(planlananHatirlaticiMetni, uyariDakikasi));
                }
                else
                {
                    MetniDegistir(arayuz.DurumHatirlaticiMetni, DilServisi.AktifDil.GetValueOrDefault("Status.ReminderShown", "Hatırlatıcı Gösterildi"));
                }
            }
            else
            {
                string planlananHatirlaticiMetni = DilServisi.AktifDil.GetValueOrDefault("Status.ReminderScheduled", "Hatırlatıcı {0} dakika kala gösterilecek");
                MetniDegistir(arayuz.DurumHatirlaticiMetni, string.Format(planlananHatirlaticiMetni, uyariDakikasi));
            }
        }
    }
}