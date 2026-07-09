using System;
using System.Windows;
using System.Windows.Controls;
using Chronoff.Core;
using Chronoff.Servis;

namespace Chronoff
{
    public partial class HatirlatmaPopup : Window
    {
        private readonly Arayuz _anaArayuz;
        private readonly int _toplamUyarSaniyesi;

        /// <summary>TR: Hatırlatıcı penceresini oluşturur. EN: Initializes the reminder window.</summary>
        public HatirlatmaPopup(Arayuz anaArayuz, int uyariDakikasi, SistemGorevi secilenGorev)
        {
            InitializeComponent();
            _anaArayuz = anaArayuz;
            _toplamUyarSaniyesi = uyariDakikasi * 60;

            if (DilServisi.AktifDil != null)
            {
                if (DilServisi.AktifDil.TryGetValue("Popup.Title", out var title)) this.Title = title;
                if (DilServisi.AktifDil.TryGetValue("Popup.Header", out var header)) BaslikText.Text = header;
                if (DilServisi.AktifDil.TryGetValue("Popup.SnoozeLabel", out var ertelemeEtiketi)) lblEylemiErtele.Text = ertelemeEtiketi;
                if (DilServisi.AktifDil.TryGetValue("Popup.Confirm", out var onaylaMetni)) btnOnayla.Content = onaylaMetni;
                if (DilServisi.AktifDil.TryGetValue("Popup.Close", out var kapatMetni)) btnKapat.Content = kapatMetni;
                if (DilServisi.AktifDil.TryGetValue("Popup.CancelAction", out var iptalMetni)) btnIptalEt.Content = iptalMetni;
            }

            string sistemGorevleri = secilenGorev switch
            {
                SistemGorevi.YenidenBaslat  => "Popup.ActionRestart",
                SistemGorevi.Sleep          => "Popup.ActionSleep",
                SistemGorevi.Hibernate      => "Popup.ActionHibernate",
                SistemGorevi.Kilitle        => "Popup.ActionLock",
                SistemGorevi.OturumuKapat   => "Popup.ActionLogoff",
                _                           => "Popup.ActionShutdown"
            };

            string gorevMetni = "kapatılacak";
            if (DilServisi.AktifDil != null && DilServisi.AktifDil.TryGetValue(sistemGorevleri, out var eylemDegeri))
            {
                gorevMetni = eylemDegeri;
            }
            else
            {
                gorevMetni = secilenGorev switch
                {
                    SistemGorevi.YenidenBaslat  => "Yeniden Başlatılacak",
                    SistemGorevi.Kilitle        => "Kilitlenecek",
                    _                           => "Kapatılacak"
                };
            }

            string hatirlatmaMetni = "Sistem {0} dakika içinde {1}.";
            if (DilServisi.AktifDil != null && DilServisi.AktifDil.TryGetValue("Popup.KalanSureText", out var sablonDegeri))
            {
                hatirlatmaMetni = sablonDegeri;
            }

            KalanSureText.Text = string.Format(hatirlatmaMetni, uyariDakikasi, gorevMetni);

            BuildSnoozeButtons();
        }

        /// <summary>TR: Erteleme butonlarını dinamik olarak oluşturur. EN: Dynamically builds snooze buttons.</summary>
        private void BuildSnoozeButtons()
        {
            if (ErtelemeSecim == null || lblEylemiErtele == null) return;

            ErtelemeSecim.Children.Clear();
            int butonSayisi = 0;

            if (_anaArayuz._ertelemeSecenekleri != null)
            {
                string saatBirimi = "saat";
                string dakikaBirimi = "dk";
                if (DilServisi.AktifDil != null)
                {
                    if (DilServisi.AktifDil.TryGetValue("Time.Hour", out var h)) saatBirimi = h;
                    if (DilServisi.AktifDil.TryGetValue("Time.Minute", out var m)) dakikaBirimi = m;
                }

                foreach (var option in _anaArayuz._ertelemeSecenekleri)
                {
                    int dakikalar = option.Unit == "Saat" ? option.Value * 60 : option.Value;
                    string text = option.Unit == "Saat" ? $"{option.Value}{saatBirimi}" : $"{option.Value}{dakikaBirimi}";

                    System.Windows.Controls.Button buton = new System.Windows.Controls.Button
                    {
                        Style = (Style)FindResource("SnoozeButton"),
                        Content = text,
                        Tag = dakikalar.ToString()
                    };
                    buton.Click += Ertele_Click;
                    ErtelemeSecim.Children.Add(buton);
                    butonSayisi++;
                }
            }

            if (butonSayisi == 0)
            {
                lblEylemiErtele.Visibility = Visibility.Collapsed;
                ErtelemeSecim.Visibility = Visibility.Collapsed;
            }
            else
            {
                lblEylemiErtele.Visibility = Visibility.Visible;
                ErtelemeSecim.Visibility = Visibility.Visible;
                ErtelemeSecim.Columns = butonSayisi;
            }
        }

        /// <summary>TR: Geri sayımı günceller. EN: Updates the countdown display.</summary>
        public void GeriSayimGuncelle(int kalanToplamSaniye)
        {
            if (kalanToplamSaniye <= 0) return;

            TimeSpan zamanDilimi = TimeSpan.FromSeconds(kalanToplamSaniye);
            CountdownDisplay.Text = zamanDilimi.ToString(@"mm\:ss");

            if (_toplamUyarSaniyesi > 0)
            {
                double yuzdeDegeri = ((double)kalanToplamSaniye / _toplamUyarSaniyesi) * 100;
                ProgressBarUyar.Value = yuzdeDegeri;
            }
        }

        private void Ertele_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button? buton = sender as System.Windows.Controls.Button;

            if (buton != null && buton.Tag != null)
            {
                string? erteleSecim = buton.Tag.ToString();

                if (erteleSecim != null && int.TryParse(erteleSecim, out int eklenecekDakika))
                {
                    _anaArayuz.SureyiErtele(eklenecekDakika);
                    this.Close();
                }
            }
        }

        private void Onayla_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Iptal_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void IptalEt_Click(object sender, RoutedEventArgs e)
        {
            if (_anaArayuz.chkParolaEtkin != null && _anaArayuz.chkParolaEtkin.IsChecked == true)
            {
                var parolaPop = new ParolaPopup(_anaArayuz._korumaParolasi);
                parolaPop.Owner = this;
                if (parolaPop.ShowDialog() == true)
                {
                    _anaArayuz.KullaniciArayuzunuSifirla();
                }
            }
            else
            {
                _anaArayuz.KullaniciArayuzunuSifirla();
            }
        }
    }
}
