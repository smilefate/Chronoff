using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Chronoff.Core;
using Chronoff.Servis;
using static Chronoff.Core.SayacServisi;
using static Chronoff.Core.SistemGorevi;
using Button = System.Windows.Controls.Button;
using ComboBox = System.Windows.Controls.ComboBox;
using RadioButton = System.Windows.Controls.RadioButton;
using TextBox = System.Windows.Controls.TextBox;

namespace Chronoff
{
    public partial class Arayuz : Window
    {
        #region Variables & Fields | Değişkenler ve Alanlar

        internal int _kalansure = 0;
        internal SistemGorevi _secilenGorev = SistemGorevi.Kapat;

        internal int _uyariDakikasi = 15;
        internal bool _hatirlaticiAktif = false;
        internal bool _popupDurumu = false;
        internal HatirlatmaPopup? _aktifPopup = null;

        internal int _aktifSekmeIndex = 1;
        internal string _korumaParolasi = "";
        internal bool _ayarlarYuklendi = false;
        internal bool _ayarlarYukleniyor = false;
        internal bool _dilYukleniyor = false;
        internal bool _isShuttingDown = false;
        internal List<ErtelemeSecenegiOgesi> _ertelemeSecenekleri = new List<ErtelemeSecenegiOgesi>();

        public static Dictionary<string, string> AktifDil => DilServisi.AktifDil;
        internal string _tarihFormati = "GG.AA.YYYY";
        private bool _baslangictaCalistirTercihi = false;
        internal string _aktifDilYolu = "";
        private string _aktifTema = "koyu";

        #endregion

        #region Constructor & Init | Yapıcı Metot ve Başlangıç

        public Arayuz()
        {
            InitializeComponent();

            string mevcutGununBolumu = DateTime.Now.Hour >= 12 ? "ÖS" : "ÖÖ";
            if (btnTarihSaatAmPm != null) btnTarihSaatAmPm.Content = mevcutGununBolumu;
            if (btnYinelemeAmPm != null) btnYinelemeAmPm.Content = mevcutGununBolumu;

            if (!System.IO.File.Exists(AyarServisi.AyarDosyaKonumu()))
            {
                bool sistemSaatBicimi = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern.Contains("h") ||
                                        System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern.Contains("t");
                if (chkOnIkiSaat != null) chkOnIkiSaat.IsChecked = sistemSaatBicimi;
            }

            System.Windows.Application.Current.ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown;

            _korumaParolasi = "";
            txtUygulamaParolasi.Password = "";
            if (txtParolaGirisAlani != null)
            {
                txtParolaGirisAlani.Visibility = Visibility.Visible;
            }

            SayacServisi.Instance.Tick += SayacServisi_Tick;
            SayacServisi.Instance.Finished += SayacServisi_Finished;

            this.Closing += Arayuz_Closing;
            this.Loaded += Arayuz_Loaded;



            DateTime bugun = DateTime.Now;
            for (int i = 1; i <= 12; i++)
            {
                TarihAyGir.Items.Add(i.ToString("D2"));
            }
            int buYil = bugun.Year;
            for (int i = buYil; i <= buYil + 10; i++)
            {
                TarihYilGir.Items.Add(i.ToString());
            }

            TarihAyGir.SelectedItem = bugun.Month.ToString("D2");
            TarihYilGir.SelectedItem = bugun.Year.ToString();

            TarihSeciciyiGuncelle();
            TarihGunGir.SelectedItem = bugun.Day.ToString("D2");

            ZamanKutusuEventleriniBagla();
            PopupDkGir.LostFocus += PopupDkGir_LostFocus;
            PopupDkGir.TextChanged += (s, e) => DurumPaneliniGuncelle();
            PopupDkGir.PreviewTextInput += SadeceRakamGirisi;

            BildirimMerkezi.Instance.Initialize(
                acmaEylemi: () =>
                {
                    this.Show();
                    this.WindowState = WindowState.Normal;
                    this.Activate();
                    BildirimMerkezi.Instance.Visible = false;
                },
                gorevIptalEylemi: () =>
                {
                    if (chkParolaEtkin != null && chkParolaEtkin.IsChecked == true)
                    {
                        var parolaPenceresi = new ParolaPopup(_korumaParolasi);
                        if (parolaPenceresi.ShowDialog() == true)
                        {
                            KullaniciArayuzunuSifirla();
                        }
                    }
                    else
                    {
                        KullaniciArayuzunuSifirla();
                    }
                },
                cikisEylemi: async () =>
                {
                    if (SayacServisi.Instance.IsEnabled && chkParolaEtkin != null && chkParolaEtkin.IsChecked == true)
                    {
                        bool parolaOnaylandi = this.Dispatcher.Invoke(() =>
                        {
                            var parolaPenceresi = new ParolaPopup(_korumaParolasi);
                            return parolaPenceresi.ShowDialog() == true;
                        });

                        if (!parolaOnaylandi)
                        {
                            return;
                        }
                    }

                    _isShuttingDown = true;
                    this.Hide();
                    await AyarlariKaydetAsync();
                    BildirimMerkezi.Instance.Dispose();
                    System.Windows.Application.Current.Shutdown();
                    System.Environment.Exit(0);
                },
                gorevCalisiyorMu: () => SayacServisi.Instance.IsEnabled
            );

            try
            {
                using (var rgAnahtar = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false))
                {
                    if (rgAnahtar != null)
                    {
                        chkBaslangictaCalistir.IsChecked = rgAnahtar.GetValue("Chronoff") != null;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Başlangıçta çalıştır kayıt defteri okunurken hata oluştu: {ex.Message}");
            }



            TarihGunGir.SelectionChanged += (s, e) => DurumPaneliniGuncelle();
            TarihAyGir.SelectionChanged += (s, e) => DurumPaneliniGuncelle();
            TarihYilGir.SelectionChanged += (s, e) => DurumPaneliniGuncelle();
            YinelemeTipi.SelectionChanged += (s, e) => DurumPaneliniGuncelle();
            AyinGunleriList.SelectionChanged += (s, e) => DurumPaneliniGuncelle();

            HaftalikGunEventleriniBagla();
            ButonEventleriniBagla();

            chkButonGoster.Checked += (s, e) => { GuncelleButonGosterimleri(); AyarlariKaydet(); };
            chkButonGoster.Unchecked += (s, e) => { GuncelleButonGosterimleri(); AyarlariKaydet(); };
            chkButonTarihSaat.Checked += (s, e) => { GuncelleButonGosterimleri(); AyarlariKaydet(); };
            chkButonTarihSaat.Unchecked += (s, e) => { GuncelleButonGosterimleri(); AyarlariKaydet(); };
            chkButonGeriSayim.Checked += (s, e) => { GuncelleButonGosterimleri(); AyarlariKaydet(); };
            chkButonGeriSayim.Unchecked += (s, e) => { GuncelleButonGosterimleri(); AyarlariKaydet(); };
            chkButonYineleme.Checked += (s, e) => { GuncelleButonGosterimleri(); AyarlariKaydet(); };
            chkButonYineleme.Unchecked += (s, e) => { GuncelleButonGosterimleri(); AyarlariKaydet(); };

            txtSaatArtis.PreviewTextInput += SadeceRakamGirisi;
            txtDakikaArtis.PreviewTextInput += SadeceRakamGirisi;

            txtSaatArtis.TextChanged += TxtSaatArtis_TextChanged;
            txtDakikaArtis.TextChanged += TxtDakikaArtis_TextChanged;

            txtSaatArtis.LostFocus += TxtSaatArtis_LostFocus;
            txtDakikaArtis.LostFocus += TxtDakikaArtis_LostFocus;

            if (chkOnIkiSaat != null)
            {
                chkOnIkiSaat.Checked += chkOnIkiSaat_Changed;
                chkOnIkiSaat.Unchecked += chkOnIkiSaat_Changed;
            }

            if (chkHatirlaticiGoster != null)
            {
                chkHatirlaticiGoster.Checked += chkHatirlaticiGoster_Changed;
                chkHatirlaticiGoster.Unchecked += chkHatirlaticiGoster_Changed;
            }
            if (chkHatirlaticiHerZaman != null)
            {
                chkHatirlaticiHerZaman.Checked += chkHatirlaticiHerZaman_Changed;
                chkHatirlaticiHerZaman.Unchecked += chkHatirlaticiHerZaman_Changed;
            }

            if (txtHatirlatmaDk != null)
            {
                txtHatirlatmaDk.PreviewTextInput += SadeceRakamGirisi;
                txtHatirlatmaDk.TextChanged += TxtHatirlatmaDk_TextChanged;
                txtHatirlatmaDk.LostFocus += TxtHatirlatmaDk_LostFocus;
            }

            if (chkArkaPlandaCalis != null)
            {
                chkArkaPlandaCalis.Checked += (s, e) => AyarlariKaydet();
                chkArkaPlandaCalis.Unchecked += (s, e) => AyarlariKaydet();
            }

            if (chkParolaEtkin != null)
            {
                chkParolaEtkin.Checked += (s, e) => { if (_ayarlarYuklendi) AyarlariKaydet(); };
                chkParolaEtkin.Unchecked += (s, e) => { if (_ayarlarYuklendi) AyarlariKaydet(); };
            }

            // Ayarları ve dili açılışta hemen yükle (Görsel kaymayı önlemek için)
            VarsayilanAyarlar();
            TarihSaatGecmisKontrolVeGuncelle();
            _ayarlarYuklendi = true;
            Microsoft.Win32.SystemEvents.UserPreferenceChanged += KullaniciTercihleriDegisti;
        }

        #endregion

        #region UI & Theme | Arayüz Kontrolleri ve Tema Yönetimi

        private void SekmeDegisti(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.Tag != null)
            {
                _aktifSekmeIndex = int.Parse(rb.Tag.ToString()!);

                // İlk yüklemede null check güvencesi
                if (SekmeTarihSaat == null || SekmeGeriSayim == null || SekmeYineleme == null) return;

                SekmeTarihSaat.Visibility = (_aktifSekmeIndex == 0) ? Visibility.Visible : Visibility.Collapsed;
                SekmeGeriSayim.Visibility = (_aktifSekmeIndex == 1) ? Visibility.Visible : Visibility.Collapsed;
                SekmeYineleme.Visibility = (_aktifSekmeIndex == 2) ? Visibility.Visible : Visibility.Collapsed;

                if (YinelemeTipi != null)
                {
                    YinelemeTipi.Visibility = (_aktifSekmeIndex == 2) ? Visibility.Visible : Visibility.Collapsed;
                }
                if (YinelemeAciklamaPanel != null)
                {
                    YinelemeAciklamaPanel.Visibility = (_aktifSekmeIndex == 2) ? Visibility.Visible : Visibility.Collapsed;
                }

                DurumPaneliniGuncelle();
            }
        }

        private void YinelemeTipi_Degisti(object sender, SelectionChangedEventArgs e)
        {
            if (YinelemeGunlukPanel == null || YinelemeGunlerPanel == null || YinelemeAyPanel == null) return;

            ComboBox cb = (ComboBox)sender;
            int index = cb.SelectedIndex;

            // 0: Günlük, 1: Haftalık, 2: Aylık
            YinelemeGunlukPanel.Visibility = (index == 0) ? Visibility.Visible : Visibility.Collapsed;
            YinelemeGunlerPanel.Visibility = (index == 1) ? Visibility.Visible : Visibility.Collapsed;
            YinelemeAyPanel.Visibility = (index == 2) ? Visibility.Visible : Visibility.Collapsed;

            if (lblYinelemeGunlukAciklama != null) lblYinelemeGunlukAciklama.Visibility = (index == 0) ? Visibility.Visible : Visibility.Collapsed;
            if (lblYinelemeHaftalikAciklama != null) lblYinelemeHaftalikAciklama.Visibility = (index == 1) ? Visibility.Visible : Visibility.Collapsed;
            if (lblYinelemeAylikAciklama != null) lblYinelemeAylikAciklama.Visibility = (index == 2) ? Visibility.Visible : Visibility.Collapsed;

            if (YinelemeButtonsPanel != null && YinelemeZamanPanel != null)
            {
                if (index == 2) // Aylık
                {
                    YinelemeButtonsPanel.Margin = new Thickness(0, 2, 0, 0); // Remove spacing between time inputs and buttons
                    YinelemeZamanPanel.Margin = new Thickness(0, 8, 0, 2);   // Add space between days grid and time inputs
                }
                else // Günlük & Haftalık
                {
                    YinelemeButtonsPanel.Margin = new Thickness(0, 12, 0, 0); // Keep spacing between time inputs and buttons
                    YinelemeZamanPanel.Margin = new Thickness(0, 2, 0, 2);    // Default margin
                }
            }
        }

        internal void TarihSeciciyiGuncelle()
        {
            if (TarihGunGir == null || TarihAyGir == null || TarihYilGir == null) return;
            if (TarihAyGir.SelectedItem == null || TarihYilGir.SelectedItem == null) return;

            if (int.TryParse(TarihAyGir.SelectedItem.ToString(), out int secilenAy) &&
                int.TryParse(TarihYilGir.SelectedItem.ToString(), out int secilenYil))
            {
                int gunSayisi = DateTime.DaysInMonth(secilenYil, secilenAy);
                string? eskiSecilenGun = TarihGunGir.SelectedItem?.ToString();

                TarihGunGir.Items.Clear();
                for (int i = 1; i <= gunSayisi; i++)
                {
                    TarihGunGir.Items.Add(i.ToString("D2"));
                }

                if (eskiSecilenGun != null && int.TryParse(eskiSecilenGun, out int eskiGun) && eskiGun <= gunSayisi)
                {
                    TarihGunGir.SelectedItem = eskiSecilenGun;
                }
                else
                {
                    TarihGunGir.SelectedIndex = 0;
                }
            }
        }

        private void TarihTipi_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TarihSeciciyiGuncelle();
        }

        private void HatirlaticiToggle_Changed(object sender, RoutedEventArgs e)
        {
            if (HatirlaticiToggle != null)
            {
                _hatirlaticiAktif = HatirlaticiToggle.IsChecked ?? false;
                if (PopupDkGir != null) PopupDkGir.IsEnabled = _hatirlaticiAktif;
            }
            DurumPaneliniGuncelle();
        }

        private void SekmeGecisi(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton buton && buton.Tag != null)
            {
                string secilenSekme = buton.Tag.ToString()!;
                if (ZamanlayiciSekmesi == null || AyarlarSekmesi == null || GorunumSekmesi == null || HakkindaSekmesi == null || YanPanel == null) return;

                ZamanlayiciSekmesi.Visibility = (secilenSekme == "timers") ? Visibility.Visible : Visibility.Collapsed;
                YanPanel.Visibility = (secilenSekme == "timers") ? Visibility.Visible : Visibility.Collapsed;
                AyarlarSekmesi.Visibility = (secilenSekme == "settings") ? Visibility.Visible : Visibility.Collapsed;
                GorunumSekmesi.Visibility = (secilenSekme == "appearance") ? Visibility.Visible : Visibility.Collapsed;
                HakkindaSekmesi.Visibility = (secilenSekme == "about") ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void Pencere_SimgeDurumu(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void TemaDegistir_Tikla(object sender, RoutedEventArgs e)
        {
            string sonrakiTema = _aktifTema == "koyu" ? "acik" : "koyu";
            TemaUygula(sonrakiTema);
        }

        private void TemaAcik_Tikla(object sender, RoutedEventArgs e)
        {
            TemaUygula("acik");
        }

        private void TemaSistem_Tikla(object sender, RoutedEventArgs e)
        {
            TemaUygula("sistem");
        }

        private void TemaKoyu_Tikla(object sender, RoutedEventArgs e)
        {
            TemaUygula("koyu");
        }

        private void TxtParolaDegisti(object sender, RoutedEventArgs e)
        {
            if (txtUygulamaParolasi != null)
            {
                _korumaParolasi = txtUygulamaParolasi.Password;
                if (txtParolaGirisAlani != null)
                {
                    txtParolaGirisAlani.Visibility = (_korumaParolasi.Length > 0) ? Visibility.Collapsed : Visibility.Visible;
                }
                if (_ayarlarYuklendi) AyarlariKaydet();
            }
        }

        private void BaslangictaCalistir_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.CheckBox chk)
            {
                if (chk.IsEnabled)
                {
                    _baslangictaCalistirTercihi = chk.IsChecked is true;
                }
                WindowsBaslangiciniAyarla(chk.IsChecked is true);
                if (_ayarlarYuklendi) AyarlariKaydet();
            }
        }

        private void KullaniciTercihleriDegisti(object sender, Microsoft.Win32.UserPreferenceChangedEventArgs e)
        {
            if (_aktifTema == "sistem")
            {
                Dispatcher.Invoke(() =>
                {
                    TemaUygula("sistem");
                });
            }
        }

        private void MaviVurgu_Tikla(object sender, RoutedEventArgs e)
        {
            Resources["PrimaryBlue"] = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0078D4"));
            if (_ayarlarYuklendi) AyarlariKaydet();
        }

        private void TurkuazVurgu_Tikla(object sender, RoutedEventArgs e)
        {
            Resources["PrimaryBlue"] = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#006687"));
            if (_ayarlarYuklendi) AyarlariKaydet();
        }

        private void TuruncuVurgu_Tikla(object sender, RoutedEventArgs e)
        {
            Resources["PrimaryBlue"] = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#974700"));
            if (_ayarlarYuklendi) AyarlariKaydet();
        }

        private void TemaUygula(string tema)
        {
            _aktifTema = tema;
            TemaYonetimi.TemaUygula(this.Resources, tema);
            if (_ayarlarYuklendi) AyarlariKaydet();
        }

        #endregion

        #region Validation & Events | Giriş Validasyonları ve Olay Bağlantıları

        private void ZamanKutusu_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox txtBox)
            {
                bool saatMi = txtBox.Tag?.ToString() == "Saat";
                string yeniZaman = ZamanBirimiYonetimi.ZamanMetniValideEt(txtBox.Text, saatMi, chkOnIkiSaat?.IsChecked == true);
                txtBox.Text = yeniZaman;
                TarihSaatGecmisKontrolVeGuncelle();
                DurumPaneliniGuncelle();
                if (_ayarlarYuklendi) AyarlariKaydet();
            }
        }


        private void PopupDkGir_LostFocus(object sender, RoutedEventArgs e)
        {
            if (PopupDkGir == null) return;
            if (int.TryParse(PopupDkGir.Text, out int dakika))
            {
                if (dakika > 59) dakika = 59;
                if (dakika < 1) dakika = 1;
                PopupDkGir.Text = dakika.ToString("D2");
            }
            else
            {
                PopupDkGir.Text = "15";
            }
            DurumPaneliniGuncelle();
            if (_ayarlarYuklendi) AyarlariKaydet();
        }


        private void HaftalikGunEventleriniBagla()
        {
            var gunler = new[] { chkPzt, chkSal, chkCar, chkPer, chkCum, chkCmt, chkPaz };
            foreach (var chk in gunler)
            {
                if (chk != null)
                {
                    chk.Checked += GunKutusu_DurumDegisti;
                    chk.Unchecked += GunKutusu_DurumDegisti;
                }
            }
        }

        private void GunKutusu_DurumDegisti(object sender, RoutedEventArgs e)
        {
            DurumPaneliniGuncelle();
        }

        private void ZamanKutusuEventleriniBagla()
        {
            var zamanKutulari = new[]
            {
                SaatGir, DakikaGir, TarihSaatGir, TarihDakikaGir, YinelemeSaat, YinelemeDakika
            };

            foreach (var txt in zamanKutulari)
            {
                if (txt != null)
                {
                    txt.LostFocus += ZamanKutusu_LostFocus;
                    txt.TextChanged += ZamanKutusu_TextChanged;
                    txt.PreviewTextInput += SadeceRakamGirisi;
                }
            }
        }

        private void ZamanKutusu_TextChanged(object sender, TextChangedEventArgs e)
        {
            DurumPaneliniGuncelle();
        }

        private void ButonEventleriniBagla()
        {
            var butonlar = new[]
            {
                btnSaatEkle, btnDakikaEkle, btnYinelemeSaatEkle, btnYinelemeDakikaEkle, btnTarihSaatEkle, btnTarihSaatDakikaEkle
            };

            foreach (var btn in butonlar)
            {
                if (btn != null)
                {
                    btn.PreviewMouseRightButtonDown += Button_PreviewMouseRightButtonDown;
                    btn.PreviewMouseRightButtonUp += Button_PreviewMouseRightButtonUp;
                    btn.MouseLeave += Button_MouseLeave;
                }
            }
        }

        #endregion

        #region Window Behaviors | Pencere Davranışları ve Temel Olaylar

        private async void Arayuz_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_isShuttingDown) return;

            bool arkaPlandaCalis = (chkArkaPlandaCalis != null && chkArkaPlandaCalis.IsChecked == true) || SayacServisi.Instance.IsEnabled;

            if (arkaPlandaCalis)
            {
                e.Cancel = true;
                this.Hide();
                BildirimMerkezi.Instance.Visible = true;
                string baslik = DilServisi.AktifDil.GetValueOrDefault("SysTray.BalloonTitle", "Chronoff Çalışıyor");
                string mesaj = DilServisi.AktifDil.GetValueOrDefault("SysTray.BalloonText", "Uygulama arka planda çalışmaya devam ediyor.");
                BildirimMerkezi.Instance.ShowBalloonTip(3000, baslik, mesaj, System.Windows.Forms.ToolTipIcon.Info);

                AyarlariKaydet();
            }
            else
            {
                e.Cancel = true;
                this.Hide();

                _isShuttingDown = true;
                await AyarlariKaydetAsync();

                BildirimMerkezi.Instance.Dispose();
                System.Windows.Application.Current.Shutdown();
                System.Environment.Exit(0);
            }
        }

        private void Arayuz_Loaded(object sender, RoutedEventArgs e)
        {
            BildirimMerkezi.Instance.Visible = false;
        }

        protected override void OnClosed(EventArgs e)
        {
            Microsoft.Win32.SystemEvents.UserPreferenceChanged -= KullaniciTercihleriDegisti;

            SayacServisi.Instance.Tick -= SayacServisi_Tick;
            SayacServisi.Instance.Finished -= SayacServisi_Finished;

            BildirimMerkezi.Instance.Dispose();
            base.OnClosed(e);
        }

        private void Baslik_Tasima(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void Pencere_Kapat(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void GorevSecimi(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.Tag != null)
            {
                _secilenGorev = AyarServisi.StringdenSistemGorevi(rb.Tag.ToString());
                if (EylemBasligi != null)
                    EylemBasligi.Text = SistemEylemiAdiniAl();
                DurumPaneliniGuncelle();
                if (_ayarlarYuklendi) AyarlariKaydet();
            }
        }

        internal void SadeceRakamGirisi(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = System.Text.RegularExpressions.Regex.IsMatch(e.Text, "[^0-9]+");
        }

        #endregion

        #region Timer Listeners | Sayaç Servisi Dinleyicileri

        private void SayacServisi_Tick(int kalan)
        {
            _kalansure = kalan;
            Geri_Sayim();
            DurumPaneliniGuncelle();

            if (_hatirlaticiAktif && _kalansure == _uyariDakikasi * 60 && !_popupDurumu)
            {
                _popupDurumu = true;
                _aktifPopup = new HatirlatmaPopup(this, _uyariDakikasi, _secilenGorev);
                _aktifPopup.Show();
            }

            if (_aktifPopup is { IsVisible: true })
            {
                _aktifPopup.GeriSayimGuncelle(_kalansure);
            }
        }

        private void SayacServisi_Finished()
        {
            _aktifPopup?.Close();
            Gorevler();
        }

        #endregion

        #region Timer Control | Sayaç Kontrolü ve Başlat/İptal İşlemleri

        public void Sayac_Ayar(object? sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: not null } tiklananButon)
            {
                string tag = tiklananButon.Tag.ToString()!;
                bool azaltmaModu = ZamanBirimiYonetimi.AzaltmaYapilacakMi(this, tiklananButon);
                int saatArtisi = ZamanBirimiYonetimi.SaatArtisiniAl(this);
                int dakikaArtisi = ZamanBirimiYonetimi.DakikaArtisiniAl(this);

                var (txtHedefSaat, txtHedefDakika) = _aktifSekmeIndex switch
                {
                    0 => (TarihSaatGir, TarihDakikaGir),
                    1 => (SaatGir, DakikaGir),
                    2 => (YinelemeSaat, YinelemeDakika),
                    _ => (null, null)
                };

                if (txtHedefSaat != null && txtHedefDakika != null)
                {
                    var (mevcutSaat, mevcutDakika) = ZamanBirimiYonetimi.DegerleriAl(txtHedefSaat, txtHedefDakika);
                    bool onIkiSaatModu = (txtHedefSaat == TarihSaatGir || txtHedefSaat == YinelemeSaat)
                                         && (chkOnIkiSaat.IsChecked == true);

                    Button btnAmPm = (txtHedefSaat == TarihSaatGir) ? btnTarihSaatAmPm : btnYinelemeAmPm;
                    string amPm = btnAmPm.Content?.ToString() ?? "ÖÖ";

                    int gun = 1, ay = 1, yil = DateTime.Today.Year;
                    string? gunMetni = TarihGunGir.SelectedItem?.ToString() ?? TarihGunGir.Text;
                    string? ayMetni = TarihAyGir.SelectedItem?.ToString() ?? TarihAyGir.Text;
                    string? yilMetni = TarihYilGir.SelectedItem?.ToString() ?? TarihYilGir.Text;
                    int.TryParse(gunMetni, out gun);
                    int.TryParse(ayMetni, out ay);
                    int.TryParse(yilMetni, out yil);

                    var sonuc = ZamanBirimiYonetimi.SayacAyarHesapla(
                        tag,
                        azaltmaModu,
                        saatArtisi,
                        dakikaArtisi,
                        _aktifSekmeIndex,
                        mevcutSaat,
                        mevcutDakika,
                        onIkiSaatModu,
                        amPm,
                        gun,
                        ay,
                        yil
                    );

                    if (sonuc.GunDegisimi != 0)
                    {
                        TarihiDegistir(sonuc.GunDegisimi);
                    }

                    if (tag.Contains("Saat"))
                    {
                        if (onIkiSaatModu)
                        {
                            txtHedefSaat.Text = sonuc.YeniSaat.ToString("D2");
                            if (btnAmPm != null) btnAmPm.Content = sonuc.YeniAmPm;
                        }
                        else
                        {
                            txtHedefSaat.Text = sonuc.YeniSaat.ToString("D2");
                        }
                    }
                    else
                    {
                        txtHedefDakika.Text = sonuc.YeniDakika.ToString("D2");
                    }
                }
            }
        }

        /// <summary>TR: Zamanlayıcıyı başlatır. EN: Starts the timer.</summary>
        public void Baslat_Tikla(object? sender, RoutedEventArgs e)
        {
            int hedefSaniye = 0;

            if (_aktifSekmeIndex == 1)
            {
                if (int.TryParse(SaatGir.Text, out int saat) && int.TryParse(DakikaGir.Text, out int dakika))
                {
                    hedefSaniye = (saat * 3600) + (dakika * 60);
                }
            }
            else if (_aktifSekmeIndex == 0)
            {
                string? gunMetni = TarihGunGir.SelectedItem?.ToString() ?? TarihGunGir.Text;
                string? ayMetni = TarihAyGir.SelectedItem?.ToString() ?? TarihAyGir.Text;
                string? yilMetni = TarihYilGir.SelectedItem?.ToString() ?? TarihYilGir.Text;

                if (int.TryParse(gunMetni, out int gun) &&
                    int.TryParse(ayMetni, out int ay) &&
                    int.TryParse(yilMetni, out int yil) &&
                    int.TryParse(TarihSaatGir.Text, out int saat) &&
                    int.TryParse(TarihDakikaGir.Text, out int dakika))
                {
                    try
                    {
                        int h24 = saat;
                        if (chkOnIkiSaat?.IsChecked == true)
                        {
                            string amPm = btnTarihSaatAmPm?.Content?.ToString() ?? "ÖÖ";
                            h24 = DurumPaneli.Convert12To24(saat, amPm);
                        }
                        DateTime secilenZaman = new DateTime(yil, ay, gun, h24, dakika, 0);
                        TimeSpan fark = secilenZaman - DateTime.Now;
                        if (fark.TotalSeconds <= 0)
                        {
                            DilServisi.Mesaj(
                                "Errors.PastDateTime",
                                "Lütfen gelecek bir tarih/saat seçin!");
                            return;
                        }
                        hedefSaniye = (int)fark.TotalSeconds;
                    }
                    catch
                    {
                        DilServisi.Mesaj(
                                "Errors.InvalidDateTime",
                                "Geçersiz bir tarih/saat girdiniz!");
                        return;
                    }
                }
            }
            else if (_aktifSekmeIndex == 2)
            {
                hedefSaniye = SonrakiTekrarSuresi();
            }

            if (hedefSaniye <= 0) return;

            if (chkHatirlaticiHerZaman?.IsChecked == true)
            {
                _hatirlaticiAktif = true;
                _uyariDakikasi = int.TryParse(txtHatirlatmaDk?.Text, out int defDk) ? defDk : 15;
            }
            else
            {
                _hatirlaticiAktif = HatirlaticiToggle?.IsChecked ?? false;
                if (_hatirlaticiAktif && int.TryParse(PopupDkGir?.Text, out int popDk))
                {
                    _uyariDakikasi = popDk;
                }
            }

            if (_hatirlaticiAktif)
            {
                if (_uyariDakikasi * 60 >= hedefSaniye)
                {
                    DilServisi.Mesaj(
                        "Errors.ReminderTooLong",
                        "Hatırlatıcı süresi kalan süreden büyük veya eşit olamaz!");
                    return;
                }
            }

            _kalansure = hedefSaniye;
            _popupDurumu = false;
            _aktifPopup = null;

            ArayuzDurumunuAyarla(false);
            Geri_Sayim();
            SayacServisi.Instance.Start(_kalansure);

            GeriSayimMetni.Visibility = Visibility.Visible;

            if (_aktifSekmeIndex == 2)
            {
                if (!BaslangicServisi.BaslangicDurumunuOku())
                {
                    WindowsBaslangiciniAyarla(true);
                }
            }
            GuncelleBaslangictaCalistirDurumu();
            if (_ayarlarYuklendi) AyarlariKaydet();
        }

        private void ArayuzDurumunuAyarla(bool durum)
        {
            Baslat.Visibility = durum ? Visibility.Visible : Visibility.Collapsed;
            Iptal.Visibility = durum ? Visibility.Collapsed : Visibility.Visible;

            SaatGir.IsEnabled = durum; DakikaGir.IsEnabled = durum;
            btnSaatEkle.IsEnabled = durum; btnDakikaEkle.IsEnabled = durum;

            TarihGunGir.IsEnabled = durum; TarihAyGir.IsEnabled = durum; TarihYilGir.IsEnabled = durum;
            TarihSaatGir.IsEnabled = durum; TarihDakikaGir.IsEnabled = durum;

            YinelemeTipi.IsEnabled = durum;
            if (YinelemeTipi != null)
            {
                YinelemeTipi.Visibility = (durum && _aktifSekmeIndex == 2) ? Visibility.Visible : Visibility.Collapsed;
            }

            if (chkPzt != null)
            {
                chkPzt.IsEnabled = durum; chkSal.IsEnabled = durum; chkCar.IsEnabled = durum; chkPer.IsEnabled = durum;
                chkCum.IsEnabled = durum; chkCmt.IsEnabled = durum; chkPaz.IsEnabled = durum;
            }

            AyinGunleriList.IsEnabled = durum;
            YinelemeSaat.IsEnabled = durum; YinelemeDakika.IsEnabled = durum;
            btnYinelemeSaatEkle.IsEnabled = durum; btnYinelemeDakikaEkle.IsEnabled = durum;
            GorevPaneli.IsEnabled = durum; HatirlaticiToggle.IsEnabled = durum;

            if (durum && _hatirlaticiAktif) PopupDkGir.IsEnabled = true; else PopupDkGir.IsEnabled = false;

            if (ZamanGirisPaneli != null && GeriSayimMetni != null)
            {
                ZamanGirisPaneli.Visibility = durum ? Visibility.Visible : Visibility.Collapsed;
                GeriSayimMetni.Visibility = durum ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        public void Iptal_Tikla(object? sender, RoutedEventArgs e)
        {
            if (chkParolaEtkin != null && chkParolaEtkin.IsChecked is true)
            {
                var parolaPenceresi = new ParolaPopup(_korumaParolasi);
                if (parolaPenceresi.ShowDialog() == true)
                {
                    KullaniciArayuzunuSifirla();
                }
            }
            else
            {
                KullaniciArayuzunuSifirla();
            }

            ArayuzDurumunuAyarla(true);
            GeriSayimMetni.Visibility = Visibility.Collapsed;
        }

        public void KullaniciArayuzunuSifirla()
        {
            SayacServisi.Instance.Stop();
            if (_aktifPopup != null)
            {
                _aktifPopup.Close();
                _aktifPopup = null;
            }

            ArayuzDurumunuAyarla(true);
            GeriSayimMetni.Visibility = Visibility.Collapsed;
            _popupDurumu = false;
            DurumPaneliniGuncelle();

            GuncelleBaslangictaCalistirDurumu();
            if (_ayarlarYuklendi) AyarlariKaydet();
        }

        public void SureyiErtele(int eklenecekDakika)
        {
            SayacServisi.Instance.Ertele(eklenecekDakika);
            _kalansure = SayacServisi.Instance.KalanSure;
            _popupDurumu = false;
            _aktifPopup = null;
            Geri_Sayim();
        }

        private void Geri_Sayim()
        {
            TimeSpan kalan_zaman = TimeSpan.FromSeconds(_kalansure);
            GeriSayimMetni.Text = kalan_zaman.ToString(@"hh\:mm\:ss");
        }

        private void Gorevler()
        {
            try
            {
                SistemEylemleri.EylemiGerceklestir(_secilenGorev);
            }
            catch (Exception ex)
            {
                DilServisi.Mesaj(
                        "Errors.SystemActionFailed",
                        $"Sistem eylemi gerçekleştirilirken hata oluştu: {ex.Message}",
                        "Errors.CriticalError", "Kritik Hata",
                        System.Windows.MessageBoxImage.Error, ex.Message);
            }
        }

        internal string SistemEylemiAdiniAl()
        {
            string anahtar = _secilenGorev switch
            {
                SistemGorevi.YenidenBaslat => "Restart",
                SistemGorevi.Sleep => "Sleep",
                SistemGorevi.Hibernate => "Hibernate",
                SistemGorevi.Kilitle => "Lock",
                SistemGorevi.OturumuKapat => "Logoff",
                _ => "Shutdown"
            };

            if (DilServisi.AktifDil != null && DilServisi.AktifDil.TryGetValue($"Sidebar.{anahtar}", out var deger))
            {
                return deger;
            }

            return _secilenGorev switch
            {
                SistemGorevi.YenidenBaslat => "Yeniden Başlat",
                SistemGorevi.Sleep => "Uyku Modu",
                SistemGorevi.Hibernate => "Hazırda Beklet",
                SistemGorevi.Kilitle => "Kilitle",
                SistemGorevi.OturumuKapat => "Oturumu Kapat",
                _ => "Bilgisayarı Kapat"
            };
        }

        #endregion

        #region Status Updates | Durum Paneli Güncelleme ve Yardımcı Yordamlar

        internal void DurumPaneliniGuncelle()
        {
            DurumPaneli.Guncelle(this);
        }

        #endregion

        #region Settings Persistence | Ayarların Yüklenmesi ve Kaydedilmesi

        internal void AyarlariKaydet()
        {
            _ = System.Threading.Tasks.Task.Run(async () =>
            {
                await AyarlariKaydetAsync();
            });
        }

        internal async System.Threading.Tasks.Task AyarlariKaydetAsync()
        {
            try
            {
                var secilenGunler = new List<int>();
                if (AyinGunleriList != null && AyinGunleriList.SelectedItems != null)
                {
                    foreach (var item in AyinGunleriList.SelectedItems)
                    {
                        if (item is ListBoxItem listBoxItem && listBoxItem.Content != null)
                        {
                            if (int.TryParse(listBoxItem.Content.ToString(), out int gunDegeri))
                            {
                                secilenGunler.Add(gunDegeri);
                            }
                        }
                    }
                }

                var ayarlar = new ChronoffAyarlar
                {
                    Theme = _aktifTema,
                    SelectedTab = _aktifSekmeIndex,
                    SelectedTask = AyarServisi.SistemGorevidenString(_secilenGorev),
                    RecurrenceActive = SayacServisi.Instance.IsEnabled && (_aktifSekmeIndex == 2),
                    RecurrenceType = YinelemeTipi.SelectedIndex,
                    RecurrenceHourText = YinelemeSaat.Text,
                    RecurrenceMinuteText = YinelemeDakika.Text,
                    Monday = chkPzt.IsChecked == true,
                    Tuesday = chkSal.IsChecked == true,
                    Wednesday = chkCar.IsChecked == true,
                    Thursday = chkPer.IsChecked == true,
                    Friday = chkCum.IsChecked == true,
                    Saturday = chkCmt.IsChecked == true,
                    Sunday = chkPaz.IsChecked == true,
                    SelectedMonthDays = secilenGunler,

                    ShowTimeIncrement = chkButonGoster.IsChecked == true,
                    ShowDateTime = chkButonTarihSaat.IsChecked == true,
                    ShowCountdown = chkButonGeriSayim.IsChecked == true,
                    ShowRecurrence = chkButonYineleme.IsChecked == true,
                    UseReverseBehavior = chkButonTersEylem.IsChecked == true,
                    HourIncrementAmount = int.TryParse(txtSaatArtis.Text, out int hVal) ? hVal : 1,
                    MinuteIncrementAmount = int.TryParse(txtDakikaArtis.Text, out int mVal) ? mVal : 5,
                    Format12Hour = chkOnIkiSaat.IsChecked == true,
                    ShowReminder = chkHatirlaticiGoster.IsChecked == true,
                    AlwaysEnableReminder = chkHatirlaticiHerZaman.IsChecked == true,
                    DefaultReminderMinutes = int.TryParse(txtHatirlatmaDk.Text, out int aM) ? aM : 15,
                    DateTimeAmPm = btnTarihSaatAmPm.Content?.ToString() ?? "ÖÖ",
                    RecurrenceAmPm = btnYinelemeAmPm.Content?.ToString() ?? "ÖÖ",
                    SnoozeOptions = _ertelemeSecenekleri,
                    LanguageFilePath = _aktifDilYolu,
                    DateFormat = _tarihFormati,
                    RunAtStartup = _baslangictaCalistirTercihi,
                    RunInBackground = chkArkaPlandaCalis.IsChecked == true,
                    CountdownHourText = SaatGir?.Text ?? "00",
                    CountdownMinuteText = DakikaGir?.Text ?? "30",
                    ReminderMinutes = PopupDkGir?.Text ?? "15",
                    PasswordProtectionEnabled = chkParolaEtkin.IsChecked == true,
                    ProtectionPassword = _korumaParolasi,
                    AccentColor = (Resources["PrimaryBlue"] as System.Windows.Media.SolidColorBrush)?.Color.ToString() ?? "#0078D4"
                };

                await AyarServisi.AyarlariKaydetAsync(ayarlar);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ayarlar kaydedilirken hata oluştu: {ex.Message}");
            }
        }

        /// <summary>TR: Ayarları yükler veya varsayılanı kullanır. EN: Loads settings or uses defaults.</summary>
        private void VarsayilanAyarlar()
        {
            try
            {
                DilleriListele();

                var ayarlar = AyarServisi.AyarlariOku();
                if (ayarlar == null)
                {
                    _aktifTema = "sistem";
                    TemaUygula("sistem");
                    rbThemeSystem.IsChecked = true;

                    MaviVurgu_Tikla(this, new RoutedEventArgs());

                    string sistemDili = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower();
                    string secilenDilDosyasi = "english.ini";
                    if (sistemDili == "tr")
                    {
                        secilenDilDosyasi = "turkish.ini";
                    }

                    bool secilenDil = false;
                    for (int i = 0; i < dilSecimKutusu.Items.Count; i++)
                    {
                        if (dilSecimKutusu.Items[i] is Diller dilOgesi && dilOgesi.DosyaYolu.EndsWith(secilenDilDosyasi, StringComparison.OrdinalIgnoreCase))
                        {
                            dilSecimKutusu.SelectedIndex = i;
                            DilUygula(dilOgesi.DosyaYolu);
                            secilenDil = true;
                            break;
                        }
                    }
                    if (!secilenDil && dilSecimKutusu.Items.Count > 0)
                    {
                        dilSecimKutusu.SelectedIndex = 0;
                        if (dilSecimKutusu.Items[0] is Diller firstL) DilUygula(firstL.DosyaYolu);
                    }

                    bool sistemSaatFormati = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern.Contains("h") ||
                                             System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern.Contains("t");
                    chkOnIkiSaat.IsChecked = sistemSaatFormati;

                    string sistemTarihBicimi = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
                    string varsayilanTarihBicimi = "GG.AA.YYYY";
                    if (sistemTarihBicimi.IndexOf('M') < sistemTarihBicimi.IndexOf('d'))
                    {
                        varsayilanTarihBicimi = "AA.GG.YYYY";
                    }
                    cmbDateFormat.SelectedIndex = (varsayilanTarihBicimi == "AA.GG.YYYY") ? 1 : 0;
                    _tarihFormati = varsayilanTarihBicimi;
                    TarihFormatiniUygula(_tarihFormati);

                    chkParolaEtkin.IsChecked = false;
                    _korumaParolasi = "";
                    txtUygulamaParolasi.Password = "";

                    chkBaslangictaCalistir.IsChecked = false;
                    _baslangictaCalistirTercihi = false;
                    WindowsBaslangiciniAyarla(false);

                    chkArkaPlandaCalis.IsChecked = true;

                    chkButonGoster.IsChecked = true;
                    chkButonTarihSaat.IsChecked = true;
                    chkButonGeriSayim.IsChecked = true;
                    chkButonYineleme.IsChecked = true;
                    chkButonTersEylem.IsChecked = true;

                    chkHatirlaticiGoster.IsChecked = true;
                    chkHatirlaticiHerZaman.IsChecked = false;

                    _ertelemeSecenekleri = new List<ErtelemeSecenegiOgesi>
                    {
                        new ErtelemeSecenegiOgesi { Value = 5, Unit = "Dakika" },
                        new ErtelemeSecenegiOgesi { Value = 10, Unit = "Dakika" },
                        new ErtelemeSecenegiOgesi { Value = 15, Unit = "Dakika" },
                        new ErtelemeSecenegiOgesi { Value = 30, Unit = "Dakika" }
                    };
                    ErtelemeListesi();

                    GuncelleButonGosterimleri();
                    ZamanMetinleri();
                    DurumPaneliniGuncelle();
                    return;
                }

                _aktifTema = ayarlar.Theme;
                TemaUygula(_aktifTema);

                if (!string.IsNullOrEmpty(ayarlar.AccentColor))
                {
                    Resources["PrimaryBlue"] = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(ayarlar.AccentColor));
                }

                rbThemeLight.IsChecked = _aktifTema == "acik";
                rbThemeSystem.IsChecked = _aktifTema == "sistem";
                rbThemeDark.IsChecked = _aktifTema == "koyu";

                _aktifSekmeIndex = ayarlar.SelectedTab;
                rbTabTarihSaat.IsChecked = _aktifSekmeIndex == 0;
                rbTabGeriSayim.IsChecked = _aktifSekmeIndex == 1;
                rbTabYineleme.IsChecked = _aktifSekmeIndex == 2;

                SekmeTarihSaat.Visibility = (_aktifSekmeIndex == 0) ? Visibility.Visible : Visibility.Collapsed;
                SekmeGeriSayim.Visibility = (_aktifSekmeIndex == 1) ? Visibility.Visible : Visibility.Collapsed;
                SekmeYineleme.Visibility = (_aktifSekmeIndex == 2) ? Visibility.Visible : Visibility.Collapsed;
                YinelemeTipi.Visibility = (_aktifSekmeIndex == 2) ? Visibility.Visible : Visibility.Collapsed;

                _secilenGorev = AyarServisi.StringdenSistemGorevi(ayarlar.SelectedTask);
                rbGorevKapat.IsChecked = _secilenGorev == SistemGorevi.Kapat;
                rbGorevYenidenBaslat.IsChecked = _secilenGorev == SistemGorevi.YenidenBaslat;
                rbGorevSleep.IsChecked = _secilenGorev == SistemGorevi.Sleep;
                rbGorevHibernate.IsChecked = _secilenGorev == SistemGorevi.Hibernate;
                rbGorevKilitle.IsChecked = _secilenGorev == SistemGorevi.Kilitle;
                rbGorevOturumuKapat.IsChecked = _secilenGorev == SistemGorevi.OturumuKapat;

                EylemBasligi.Text = SistemEylemiAdiniAl();

                YinelemeTipi.SelectedIndex = ayarlar.RecurrenceType;
                YinelemeSaat.Text = ayarlar.RecurrenceHourText;
                YinelemeDakika.Text = ayarlar.RecurrenceMinuteText;

                chkPzt.IsChecked = ayarlar.Monday;
                chkSal.IsChecked = ayarlar.Tuesday;
                chkCar.IsChecked = ayarlar.Wednesday;
                chkPer.IsChecked = ayarlar.Thursday;
                chkCum.IsChecked = ayarlar.Friday;
                chkCmt.IsChecked = ayarlar.Saturday;
                chkPaz.IsChecked = ayarlar.Sunday;

                if (ayarlar.SelectedMonthDays != null)
                {
                    AyinGunleriList.SelectedItems.Clear();
                    foreach (ListBoxItem listeOgesi in AyinGunleriList.Items)
                    {
                        if (int.TryParse(listeOgesi.Content.ToString(), out int gunDegeri) && ayarlar.SelectedMonthDays.Contains(gunDegeri))
                        {
                            AyinGunleriList.SelectedItems.Add(listeOgesi);
                        }
                    }
                }

                if (ayarlar.RecurrenceActive)
                {
                    _aktifSekmeIndex = 2;
                    Baslat_Tikla(null, new RoutedEventArgs());

                    WindowsBaslangiciniAyarla(true);
                    chkBaslangictaCalistir.IsChecked = true;
                    chkBaslangictaCalistir.IsEnabled = false;
                }

                chkButonGoster.IsChecked = ayarlar.ShowTimeIncrement;
                chkButonTarihSaat.IsChecked = ayarlar.ShowDateTime;
                chkButonGeriSayim.IsChecked = ayarlar.ShowCountdown;
                chkButonYineleme.IsChecked = ayarlar.ShowRecurrence;
                chkButonTersEylem.IsChecked = ayarlar.UseReverseBehavior;
                txtSaatArtis.Text = ayarlar.HourIncrementAmount.ToString();
                txtDakikaArtis.Text = ayarlar.MinuteIncrementAmount.ToString();
                chkArkaPlandaCalis.IsChecked = ayarlar.RunInBackground;

                _ayarlarYukleniyor = true;

                chkOnIkiSaat.IsChecked = ayarlar.Format12Hour;
                chkHatirlaticiGoster.IsChecked = ayarlar.ShowReminder;
                chkHatirlaticiHerZaman.IsChecked = ayarlar.AlwaysEnableReminder;
                txtHatirlatmaDk.Text = ayarlar.DefaultReminderMinutes.ToString();

                if (!string.IsNullOrEmpty(ayarlar.DateTimeAmPm))
                {
                    btnTarihSaatAmPm.Content = ayarlar.DateTimeAmPm;
                }
                if (!string.IsNullOrEmpty(ayarlar.RecurrenceAmPm))
                {
                    btnYinelemeAmPm.Content = ayarlar.RecurrenceAmPm;
                }

                if (PopupDkGir != null && !string.IsNullOrEmpty(ayarlar.ReminderMinutes))
                    PopupDkGir.Text = ayarlar.ReminderMinutes;

                if (ayarlar.SnoozeOptions != null && ayarlar.SnoozeOptions.Count > 0)
                {
                    _ertelemeSecenekleri = ayarlar.SnoozeOptions;
                }
                else
                {
                    _ertelemeSecenekleri = new List<ErtelemeSecenegiOgesi>
                    {
                        new ErtelemeSecenegiOgesi { Value = 5, Unit = "Dakika" },
                        new ErtelemeSecenegiOgesi { Value = 10, Unit = "Dakika" },
                        new ErtelemeSecenegiOgesi { Value = 30, Unit = "Dakika" },
                        new ErtelemeSecenegiOgesi { Value = 1, Unit = "Saat" }
                    };
                }
                ErtelemeListesi();

                _baslangictaCalistirTercihi = ayarlar.RunAtStartup;
                GuncelleBaslangictaCalistirDurumu();

                _tarihFormati = string.IsNullOrEmpty(ayarlar.DateFormat) ? "GG.AA.YYYY" : ayarlar.DateFormat;
                if (cmbDateFormat != null)
                {
                    cmbDateFormat.SelectedIndex = (_tarihFormati == "AA.GG.YYYY") ? 1 : 0;
                }
                TarihFormatiniUygula(_tarihFormati);

                if (SaatGir != null && !string.IsNullOrEmpty(ayarlar.CountdownHourText))
                    SaatGir.Text = ayarlar.CountdownHourText;
                if (DakikaGir != null && !string.IsNullOrEmpty(ayarlar.CountdownMinuteText))
                    DakikaGir.Text = ayarlar.CountdownMinuteText;

                // Parola ayarlarını yükle
                chkParolaEtkin.IsChecked = ayarlar.PasswordProtectionEnabled;
                _korumaParolasi = ayarlar.ProtectionPassword ?? "";
                txtUygulamaParolasi.Password = _korumaParolasi;
                if (txtParolaGirisAlani != null)
                {
                    txtParolaGirisAlani.Visibility = (_korumaParolasi.Length > 0) ? Visibility.Collapsed : Visibility.Visible;
                }

                string yuklenenDilYolu = ayarlar.LanguageFilePath;

                if (!string.IsNullOrEmpty(yuklenenDilYolu))
                {
                    // Eski .json uzantılı yolları .ini'ye dönüştür.
                    if (yuklenenDilYolu.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    {
                        if (yuklenenDilYolu.Contains("tr") || yuklenenDilYolu.Contains("turkish"))
                            yuklenenDilYolu = "turkish.ini";
                        else
                            yuklenenDilYolu = "english.ini";
                    }

                    // Tam yol kaydedilmişse sadece dosya adını al (taşınabilirlik).
                    string dilDosyaAdi = Path.GetFileName(yuklenenDilYolu);

                    // Önce listede eşleşen dili bulmaya çalış.
                    bool dilBulundu = false;
                    for (int i = 0; i < dilSecimKutusu.Items.Count; i++)
                    {
                        if (dilSecimKutusu.Items[i] is Diller dilOgesi &&
                            string.Equals(Path.GetFileName(dilOgesi.DosyaYolu), dilDosyaAdi, StringComparison.OrdinalIgnoreCase))
                        {
                            dilSecimKutusu.SelectedIndex = i;
                            DilUygula(dilOgesi.DosyaYolu);
                            dilBulundu = true;
                            break;
                        }
                    }

                    if (!dilBulundu)
                    {
                        // Önce listede English var mı ara.
                        bool englishBulundu = false;
                        for (int i = 0; i < dilSecimKutusu.Items.Count; i++)
                        {
                            if (dilSecimKutusu.Items[i] is Diller dilOgesi &&
                                (dilOgesi.DilKodu.Equals("en", StringComparison.OrdinalIgnoreCase) ||
                                 Path.GetFileName(dilOgesi.DosyaYolu).Equals("english.ini", StringComparison.OrdinalIgnoreCase)))
                            {
                                dilSecimKutusu.SelectedIndex = i;
                                DilUygula(dilOgesi.DosyaYolu);
                                englishBulundu = true;
                                break;
                            }
                        }

                        // English de listede yoksa ilk dili seç, o da yoksa doğrudan english.ini yükle.
                        if (!englishBulundu)
                        {
                            if (dilSecimKutusu.Items.Count > 0)
                            {
                                dilSecimKutusu.SelectedIndex = 0;
                                if (dilSecimKutusu.Items[0] is Diller firstL) DilUygula(firstL.DosyaYolu);
                            }
                            else
                            {
                                DilUygula("english.ini");
                            }
                        }
                    }
                }
                else if (dilSecimKutusu.Items.Count > 0)
                {
                    dilSecimKutusu.SelectedIndex = 0;
                    if (dilSecimKutusu.Items[0] is Diller firstL) DilUygula(firstL.DosyaYolu);
                }

                GuncelleButonGosterimleri();
                ZamanMetinleri();

                _ayarlarYukleniyor = false;

                DurumPaneliniGuncelle();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Varsayılan ayarlar yüklenirken hata oluştu: {ex.Message}");
            }
        }

        #endregion

        #region Recurrence | Yineleme ve Başlangıç Ayarları

        private void WindowsBaslangiciniAyarla(bool etkinlestir)
        {
            BaslangicServisi.WindowsBaslangiciniAyarla(etkinlestir);
        }

        private void GuncelleBaslangictaCalistirDurumu()
        {
            if (chkBaslangictaCalistir == null) return;

            bool haftalikVeyaAylikYinelemeMi = SayacServisi.Instance.IsEnabled && (_aktifSekmeIndex == 2) &&
                (YinelemeTipi.SelectedIndex == (int)RecurrenceType.Weekly || YinelemeTipi.SelectedIndex == (int)RecurrenceType.Monthly);
            if (haftalikVeyaAylikYinelemeMi)
            {
                chkBaslangictaCalistir.IsChecked = true;
                chkBaslangictaCalistir.IsEnabled = false;
                WindowsBaslangiciniAyarla(true);
            }
            else
            {
                chkBaslangictaCalistir.IsEnabled = true;
                chkBaslangictaCalistir.IsChecked = _baslangictaCalistirTercihi;
                WindowsBaslangiciniAyarla(_baslangictaCalistirTercihi);
            }
        }

        /// <summary>TR: Sonraki yineleme süresini hesaplar. EN: Calculates the next recurrence duration.</summary>
        private int SonrakiTekrarSuresi()
        {
            if (!int.TryParse(YinelemeSaat.Text, out int hedefSaat) ||
                !int.TryParse(YinelemeDakika.Text, out int hedefDakika))
            {
                return 0;
            }

            if (chkOnIkiSaat?.IsChecked == true)
            {
                string amPm = btnYinelemeAmPm?.Content?.ToString() ?? "ÖÖ";
                hedefSaat = DurumPaneli.Convert12To24(hedefSaat, amPm);
            }

            DateTime suAn = DateTime.Now;
            var recurrence = (RecurrenceType)YinelemeTipi.SelectedIndex;

            if (recurrence == RecurrenceType.Daily)
            {
                DateTime sonrakiCalisma = suAn.Date.AddHours(hedefSaat).AddMinutes(hedefDakika);
                if (sonrakiCalisma <= suAn)
                {
                    sonrakiCalisma = sonrakiCalisma.AddDays(1);
                }
                return (int)(sonrakiCalisma - suAn).TotalSeconds;
            }
            else if (recurrence == RecurrenceType.Weekly)
            {
                bool[] aktifGunler = new bool[7];
                aktifGunler[(int)DayOfWeek.Monday] = chkPzt.IsChecked == true;
                aktifGunler[(int)DayOfWeek.Tuesday] = chkSal.IsChecked == true;
                aktifGunler[(int)DayOfWeek.Wednesday] = chkCar.IsChecked == true;
                aktifGunler[(int)DayOfWeek.Thursday] = chkPer.IsChecked == true;
                aktifGunler[(int)DayOfWeek.Friday] = chkCum.IsChecked == true;
                aktifGunler[(int)DayOfWeek.Saturday] = chkCmt.IsChecked == true;
                aktifGunler[(int)DayOfWeek.Sunday] = chkPaz.IsChecked == true;

                if (!aktifGunler.Any(d => d))
                {
                    DateTime sonrakiCalisma = suAn.Date.AddHours(hedefSaat).AddMinutes(hedefDakika);
                    if (sonrakiCalisma <= suAn) sonrakiCalisma = sonrakiCalisma.AddDays(1);
                    return (int)(sonrakiCalisma - suAn).TotalSeconds;
                }

                for (int gunEkle = 0; gunEkle <= 7; gunEkle++)
                {
                    DateTime siradakiZaman = suAn.Date.AddDays(gunEkle).AddHours(hedefSaat).AddMinutes(hedefDakika);
                    if (siradakiZaman <= suAn) continue;

                    if (aktifGunler[(int)siradakiZaman.DayOfWeek])
                    {
                        return (int)(siradakiZaman - suAn).TotalSeconds;
                    }
                }
            }
            else if (recurrence == RecurrenceType.Monthly)
            {
                var hedefGunler = AyinGunleriList.SelectedItems
                    .Cast<ListBoxItem>()
                    .Where(oge => int.TryParse(oge.Content.ToString(), out int sayi) && sayi > 0 && sayi <= 31)
                    .Select(oge => int.Parse(oge.Content.ToString()!))
                    .OrderBy(sayi => sayi)
                    .ToList();

                if (hedefGunler.Count == 0)
                {
                    DateTime sonrakiCalisma = suAn.Date.AddHours(hedefSaat).AddMinutes(hedefDakika);
                    if (sonrakiCalisma <= suAn) sonrakiCalisma = sonrakiCalisma.AddDays(1);
                    return (int)(sonrakiCalisma - suAn).TotalSeconds;
                }

                for (int gunEkle = 0; gunEkle <= 366; gunEkle++)
                {
                    DateTime siradakiTarih = suAn.Date.AddDays(gunEkle);
                    int kullanilacakGun = siradakiTarih.Day;
                    if (hedefGunler.Contains(kullanilacakGun))
                    {
                        DateTime siradakiZaman = siradakiTarih.Date.AddHours(hedefSaat).AddMinutes(hedefDakika);
                        if (siradakiZaman <= suAn) continue;
                        return (int)(siradakiZaman - suAn).TotalSeconds;
                    }
                }
            }

            return 0;
        }

        #endregion

        #region Layout & Decrement | Buton Görünümleri ve Azaltma Modu İşlemleri

        private void TarihSaatGecmisKontrolVeGuncelle()
        {
            if (TarihGunGir == null || TarihAyGir == null || TarihYilGir == null ||
                TarihSaatGir == null || TarihDakikaGir == null) return;

            string? gunMetni = TarihGunGir.SelectedItem?.ToString() ?? TarihGunGir.Text;
            string? ayMetni = TarihAyGir.SelectedItem?.ToString() ?? TarihAyGir.Text;
            string? yilMetni = TarihYilGir.SelectedItem?.ToString() ?? TarihYilGir.Text;

            if (!int.TryParse(gunMetni, out int gun) ||
                !int.TryParse(ayMetni, out int ay) ||
                !int.TryParse(yilMetni, out int yil) ||
                !int.TryParse(TarihSaatGir.Text, out int saat) ||
                !int.TryParse(TarihDakikaGir.Text, out int dakika)) return;

            bool onIkiSaatMi = chkOnIkiSaat?.IsChecked == true;
            string amPm = btnTarihSaatAmPm?.Content?.ToString() ?? "ÖÖ";

            var sonuc = ZamanBirimiYonetimi.GecmisTarihiGuncelle(gun, ay, yil, saat, dakika, onIkiSaatMi, amPm);
            if (sonuc == null) return;

            bool yuklemeDurumu = _ayarlarYuklendi;
            _ayarlarYuklendi = false;

            TarihAyGir.SelectedItem = sonuc.Ay.ToString("D2");
            TarihYilGir.SelectedItem = sonuc.Yil.ToString();
            TarihSeciciyiGuncelle();
            TarihGunGir.SelectedItem = sonuc.Gun.ToString("D2");
            TarihSaatGir.Text = sonuc.Saat.ToString("D2");
            TarihDakikaGir.Text = sonuc.Dakika.ToString("D2");
            if (sonuc.AmPm != null && btnTarihSaatAmPm != null)
                btnTarihSaatAmPm.Content = sonuc.AmPm;

            _ayarlarYuklendi = yuklemeDurumu;
        }

        private void GuncelleButonGosterimleri()
        {
            if (chkButonGoster == null || chkButonTarihSaat == null ||
                chkButonGeriSayim == null || chkButonYineleme == null) return;

            bool anaGosterim = chkButonGoster.IsChecked == true;

            if (TarihSaatButtonsPanel != null)
            {
                TarihSaatButtonsPanel.Visibility = (anaGosterim && chkButonTarihSaat.IsChecked == true)
                    ? Visibility.Visible : Visibility.Collapsed;
            }

            if (GeriSayimButtonsPanel != null)
            {
                GeriSayimButtonsPanel.Visibility = (anaGosterim && chkButonGeriSayim.IsChecked == true)
                    ? Visibility.Visible : Visibility.Collapsed;
            }

            if (YinelemeButtonsPanel != null)
            {
                YinelemeButtonsPanel.Visibility = (anaGosterim && chkButonYineleme.IsChecked == true)
                    ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void ZamanMetinleri()
        {
            int saatArtisi = ZamanBirimiYonetimi.SaatArtisiniAl(this);
            int dakikaArtisi = ZamanBirimiYonetimi.DakikaArtisiniAl(this);

            string saatFormati = DilServisi.AktifDil?.GetValueOrDefault("MainScreen.AddHour", "+ {0} Saat") ?? "+ {0} Saat";
            string dakikaFormati = DilServisi.AktifDil?.GetValueOrDefault("MainScreen.AddMinute", "+ {0} Dk") ?? "+ {0} Dk";

            string saatMetni = string.Format(saatFormati, saatArtisi);
            string dakikaMetni = string.Format(dakikaFormati, dakikaArtisi);

            if (btnSaatEkle != null) btnSaatEkle.Content = saatMetni;
            if (btnDakikaEkle != null) btnDakikaEkle.Content = dakikaMetni;

            if (btnYinelemeSaatEkle != null) btnYinelemeSaatEkle.Content = saatMetni;
            if (btnYinelemeDakikaEkle != null) btnYinelemeDakikaEkle.Content = dakikaMetni;

            if (btnTarihSaatEkle != null) btnTarihSaatEkle.Content = saatMetni;
            if (btnTarihSaatDakikaEkle != null) btnTarihSaatDakikaEkle.Content = dakikaMetni;
        }

        private void AzaltmaEfekti(Button buton, bool azaltmaModu)
        {
            if (azaltmaModu)
            {
                string icerikMetni = buton.Content?.ToString() ?? "";
                if (icerikMetni.Contains("+"))
                {
                    buton.Content = icerikMetni.Replace("+", "-");
                }

                buton.Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#4DFF3B30"));
            }
            else
            {
                string icerikMetni = buton.Content?.ToString() ?? "";
                if (icerikMetni.Contains("-"))
                {
                    buton.Content = icerikMetni.Replace("-", "+");
                }

                buton.ClearValue(Button.BackgroundProperty);
            }
        }

        private void Button_PreviewMouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (chkButonTersEylem?.IsChecked == true && sender is Button buton)
            {
                AzaltmaEfekti(buton, true);
                Sayac_Ayar(buton, new RoutedEventArgs());
                e.Handled = true;
            }
        }

        private void Button_PreviewMouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Button buton)
            {
                AzaltmaEfekti(buton, false);
            }
        }

        private void Button_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is Button buton)
            {
                AzaltmaEfekti(buton, false);
            }
        }

        #endregion

        #region Input Listeners | Giriş Alanı Değişiklik Dinleyicileri

        private void TxtSaatArtis_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtSaatArtis == null) return;
            if (int.TryParse(txtSaatArtis.Text, out int h))
            {
                if (h > 23)
                {
                    txtSaatArtis.Text = "23";
                    txtSaatArtis.SelectionStart = txtSaatArtis.Text.Length;
                }
                else if (h < 0)
                {
                    txtSaatArtis.Text = "0";
                    txtSaatArtis.SelectionStart = txtSaatArtis.Text.Length;
                }
            }
            ZamanMetinleri();
            if (_ayarlarYuklendi) AyarlariKaydet();
        }

        private void TxtDakikaArtis_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtDakikaArtis == null) return;
            if (int.TryParse(txtDakikaArtis.Text, out int m))
            {
                if (m > 59)
                {
                    txtDakikaArtis.Text = "59";
                    txtDakikaArtis.SelectionStart = txtDakikaArtis.Text.Length;
                }
                else if (m < 0)
                {
                    txtDakikaArtis.Text = "0";
                    txtDakikaArtis.SelectionStart = txtDakikaArtis.Text.Length;
                }
            }
            ZamanMetinleri();
            if (_ayarlarYuklendi) AyarlariKaydet();
        }

        private void TxtSaatArtis_LostFocus(object sender, RoutedEventArgs e)
        {
            if (txtSaatArtis == null) return;
            if (!int.TryParse(txtSaatArtis.Text, out int h) || h <= 0)
            {
                txtSaatArtis.Text = "1";
            }
            ZamanMetinleri();
            if (_ayarlarYuklendi) AyarlariKaydet();
        }

        private void TxtDakikaArtis_LostFocus(object sender, RoutedEventArgs e)
        {
            if (txtDakikaArtis == null) return;
            if (!int.TryParse(txtDakikaArtis.Text, out int m) || m <= 0)
            {
                txtDakikaArtis.Text = "5";
            }
            ZamanMetinleri();
            if (_ayarlarYuklendi) AyarlariKaydet();
        }

        private void chkHatirlaticiGoster_Changed(object sender, RoutedEventArgs e)
        {
            DurumPaneli.HatirlaticiGorunumu(this);
            if (_ayarlarYuklendi) AyarlariKaydet();
        }

        private void chkHatirlaticiHerZaman_Changed(object sender, RoutedEventArgs e)
        {
            DurumPaneliniGuncelle();
            if (_ayarlarYuklendi) AyarlariKaydet();
        }

        private void TxtHatirlatmaDk_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtHatirlatmaDk == null) return;
            if (int.TryParse(txtHatirlatmaDk.Text, out int val))
            {
                if (val > 59)
                {
                    txtHatirlatmaDk.Text = "59";
                    txtHatirlatmaDk.SelectionStart = txtHatirlatmaDk.Text.Length;
                }
                else if (val < 1)
                {
                    txtHatirlatmaDk.Text = "1";
                    txtHatirlatmaDk.SelectionStart = txtHatirlatmaDk.Text.Length;
                }
            }
            DurumPaneliniGuncelle();
            if (_ayarlarYuklendi) AyarlariKaydet();
        }

        private void TxtHatirlatmaDk_LostFocus(object sender, RoutedEventArgs e)
        {
            if (txtHatirlatmaDk == null) return;
            if (!int.TryParse(txtHatirlatmaDk.Text, out int val) || val <= 0)
            {
                txtHatirlatmaDk.Text = "15";
            }
            DurumPaneliniGuncelle();
            if (_ayarlarYuklendi) AyarlariKaydet();
        }

        #endregion

        #region Localization & Snooze | Dil ve Erteleme Ayarları

        private void btnAmPm_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button buton)
            {
                string saatAM = DilServisi.AktifDil.GetValueOrDefault("Time.Am", "ÖÖ");
                string saatPM = DilServisi.AktifDil.GetValueOrDefault("Time.Pm", "ÖS");

                string mevcutAmPm = buton.Content?.ToString() ?? saatAM;
                string sonrakiAmPm = (mevcutAmPm == saatAM) ? saatPM : saatAM;
                buton.Content = sonrakiAmPm;
                DurumPaneliniGuncelle();
                if (_ayarlarYuklendi) AyarlariKaydet();
            }
        }

        internal void ErtelemeListesi()
        {
            ErtelemePaneliYonetici.ErtelemeListesiOlustur(this);
        }

        private void btnErtelemeSecenegiEkle_Tikla(object sender, RoutedEventArgs e)
        {
            if (_ertelemeSecenekleri.Count < 4)
            {
                _ertelemeSecenekleri.Add(new ErtelemeSecenegiOgesi { Value = 5, Unit = "Dakika" });
                ErtelemeListesi();
                if (_ayarlarYuklendi) AyarlariKaydet();
            }
        }

        private void btnErtelemeSifirla_Tikla(object sender, RoutedEventArgs e)
        {
            bool yuklemeDurumu = _ayarlarYuklendi;
            _ayarlarYuklendi = false;

            _ertelemeSecenekleri = new List<ErtelemeSecenegiOgesi>
            {
                new ErtelemeSecenegiOgesi { Value = 5, Unit = "Dakika" },
                new ErtelemeSecenegiOgesi { Value = 10, Unit = "Dakika" },
                new ErtelemeSecenegiOgesi { Value = 30, Unit = "Dakika" },
                new ErtelemeSecenegiOgesi { Value = 1, Unit = "Saat" }
            };
            ErtelemeListesi();

            _ayarlarYuklendi = yuklemeDurumu;
            AyarlariKaydet();
        }

        /// <summary>TR: Tarihi değiştirir. EN: Changes the date.</summary>
        internal void TarihiDegistir(int gunSayisi)
        {
            if (TarihGunGir == null || TarihAyGir == null || TarihYilGir == null) return;

            string? gunMetni = TarihGunGir.SelectedItem?.ToString() ?? TarihGunGir.Text;
            string? ayMetni = TarihAyGir.SelectedItem?.ToString() ?? TarihAyGir.Text;
            string? yilMetni = TarihYilGir.SelectedItem?.ToString() ?? TarihYilGir.Text;

            if (!int.TryParse(gunMetni, out int gun) ||
                !int.TryParse(ayMetni, out int ay) ||
                !int.TryParse(yilMetni, out int yil)) return;

            DateTime? yeniTarih = ZamanBirimiYonetimi.TarihiHesapla(gun, ay, yil, gunSayisi);
            if (yeniTarih == null) return;

            bool yuklemeDurumu = _ayarlarYuklendi;
            _ayarlarYuklendi = false;

            TarihYilGir.SelectedItem = yeniTarih.Value.Year.ToString();
            TarihAyGir.SelectedItem = yeniTarih.Value.Month.ToString("D2");
            TarihSeciciyiGuncelle();
            TarihGunGir.SelectedItem = yeniTarih.Value.Day.ToString("D2");

            _ayarlarYuklendi = yuklemeDurumu;
        }

        private void chkOnIkiSaat_Changed(object sender, RoutedEventArgs e)
        {
            DurumPaneli.SaatEtiketiniGuncelle(this);

            if (_ayarlarYukleniyor)
            {
                DurumPaneliniGuncelle();
                return;
            }

            bool onIkiSaatAktif = chkOnIkiSaat?.IsChecked == true;

            if (TarihSaatGir != null && int.TryParse(TarihSaatGir.Text, out int mevcutSaat))
            {
                if (onIkiSaatAktif)
                {
                    var (h12, amPm) = DurumPaneli.Convert24To12(mevcutSaat);
                    TarihSaatGir.Text = h12.ToString("D2");
                    if (btnTarihSaatAmPm != null) btnTarihSaatAmPm.Content = amPm;
                }
                else
                {
                    string amPm = btnTarihSaatAmPm?.Content?.ToString() ?? "ÖÖ";
                    int saat24 = DurumPaneli.Convert12To24(mevcutSaat, amPm);
                    TarihSaatGir.Text = saat24.ToString("D2");
                }
            }

            if (YinelemeSaat != null && int.TryParse(YinelemeSaat.Text, out int yinelemeSaati))
            {
                if (onIkiSaatAktif)
                {
                    var (h12, amPm) = DurumPaneli.Convert24To12(yinelemeSaati);
                    YinelemeSaat.Text = h12.ToString("D2");
                    if (btnYinelemeAmPm != null) btnYinelemeAmPm.Content = amPm;
                }
                else
                {
                    string amPm = btnYinelemeAmPm?.Content?.ToString() ?? "ÖÖ";
                    int saat24 = DurumPaneli.Convert12To24(yinelemeSaati, amPm);
                    YinelemeSaat.Text = saat24.ToString("D2");
                }
            }

            DurumPaneliniGuncelle();
            if (_ayarlarYuklendi) AyarlariKaydet();
        }

        private void cmbLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dilSecimKutusu.SelectedItem is Diller dilOgesi)
            {
                DilUygula(dilOgesi.DosyaYolu);
                if (_ayarlarYuklendi) AyarlariKaydet();
            }
        }

        private void cmbDateFormat_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbDateFormat != null)
            {
                int secilenIndeks = cmbDateFormat.SelectedIndex;
                string tarihBicimi = secilenIndeks == 1 ? "AA.GG.YYYY" : "GG.AA.YYYY";
                _tarihFormati = tarihBicimi;
                TarihFormatiniUygula(tarihBicimi);
                DurumPaneliniGuncelle();
                if (_ayarlarYuklendi) AyarlariKaydet();
            }
        }

        private void TarihFormatiniUygula(string format)
        {
            if (TarihGunGir == null || TarihAyGir == null || TarihYilGir == null || TarihNokta1 == null || TarihNokta2 == null) return;

            if (format == "AA.GG.YYYY")
            {
                Grid.SetColumn(TarihAyGir, 0);
                Grid.SetColumn(TarihNokta1, 1);
                Grid.SetColumn(TarihGunGir, 2);
                Grid.SetColumn(TarihNokta2, 3);
                Grid.SetColumn(TarihYilGir, 4);
            }
            else
            {
                Grid.SetColumn(TarihGunGir, 0);
                Grid.SetColumn(TarihNokta1, 1);
                Grid.SetColumn(TarihAyGir, 2);
                Grid.SetColumn(TarihNokta2, 3);
                Grid.SetColumn(TarihYilGir, 4);
            }
        }

        /// <summary>TR: Seçilen dil dosyasını yükler. EN: Loads the selected language file.</summary>
        private void DilUygula(string dosyaYolu)
        {
            if (_dilYukleniyor) return;
            _dilYukleniyor = true;
            try
            {
                var dilPaketi = DilServisi.ParseIniFile(dosyaYolu);
                if (dilPaketi.Count == 0)
                {
                    string appPath = AppDomain.CurrentDomain.BaseDirectory;
                    string englishPath = Path.Combine(appPath, "Languages", "english.ini");
                    dilPaketi = DilServisi.ParseIniFile(englishPath);
                    dosyaYolu = englishPath;
                }

                if (dilPaketi.Count == 0) return;

                DilServisi.AktifDil = dilPaketi;
                _aktifDilYolu = Path.GetFileName(dosyaYolu);

                DilServisi.ArayuzuYerellestir(this, dilPaketi);

                BildirimMerkezi.Instance.Text = "Chronoff";

                if (EylemBasligi != null) EylemBasligi.Text = SistemEylemiAdiniAl();

                DurumPaneliniGuncelle();
                ErtelemeListesi();

                if (dilSecimKutusu != null)
                {
                    for (int i = 0; i < dilSecimKutusu.Items.Count; i++)
                    {
                        if (dilSecimKutusu.Items[i] is Diller dilOgesi &&
                            string.Equals(Path.GetFileName(dilOgesi.DosyaYolu), Path.GetFileName(dosyaYolu), StringComparison.OrdinalIgnoreCase))
                        {
                            if (dilSecimKutusu.SelectedIndex != i)
                            {
                                dilSecimKutusu.SelectedIndex = i;
                            }
                            break;
                        }
                    }
                }
            }
            finally
            {
                _dilYukleniyor = false;
            }
        }

        private void DilleriListele()
        {
            if (dilSecimKutusu == null) return;
            dilSecimKutusu.Items.Clear();

            var dilListesi = DilServisi.DilleriListele();
            foreach (var dilOgesi in dilListesi)
            {
                dilSecimKutusu.Items.Add(dilOgesi);
            }

            if (dilSecimKutusu.Items.Count == 0)
            {
                dilSecimKutusu.Items.Add(new Diller { DosyaYolu = "Languages\\turkish.ini", DilKodu = "tr", Ulke = "Türkiye", DilAdi = "Türkçe" });
                dilSecimKutusu.Items.Add(new Diller { DosyaYolu = "Languages\\english.ini", DilKodu = "en", Ulke = "United States", DilAdi = "English" });
            }
        }

        private void BtnGithub_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/smilefate",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Github linki açılamadı: {ex.Message}");
            }
        }

        private void BtnCheckUpdates_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/smilefate/Chronoff/releases",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Güncelleme linki açılamadı: {ex.Message}");
            }
        }

        #endregion
    }
}