using System;
using System.Windows;
using System.Windows.Controls;
using Chronoff.Servis;

namespace Chronoff
{
    public partial class ParolaPopup : Window
    {
        private readonly string _dogruParola;
        private bool _parolaKontrolu = false;

        /// <summary>TR: Parola onay penceresini oluşturur. EN: Initializes the password confirmation window.</summary>
        public ParolaPopup(string dogruParola)
        {
            InitializeComponent();
            _dogruParola = dogruParola;

            if (DilServisi.AktifDil != null)
            {
                if (DilServisi.AktifDil.TryGetValue("Password.Title", out var baslik)) this.Title = baslik;
                if (DilServisi.AktifDil.TryGetValue("Password.Header", out var anaBaslik)) lblHeader.Text = anaBaslik;
                if (DilServisi.AktifDil.TryGetValue("Password.Subheader", out var altBaslik)) lblSubheader.Text = altBaslik;
                if (DilServisi.AktifDil.TryGetValue("Password.Label", out var etiket)) lblPasswordLabel.Text = etiket;
                if (DilServisi.AktifDil.TryGetValue("Password.Error", out var hataMetni)) lblErrorText.Text = hataMetni;
                if (DilServisi.AktifDil.TryGetValue("Password.Info", out var bilgiMetni)) lblInfoText.Text = bilgiMetni;
                if (DilServisi.AktifDil.TryGetValue("Password.Cancel", out var iptalMetni)) btnCancel.Content = iptalMetni;
                if (DilServisi.AktifDil.TryGetValue("Password.Verify", out var dogrulaMetni)) btnVerify.Content = dogrulaMetni;
            }

            txtParola.Focus();
        }

        private string GetParola()
        {
            if (btnToggleShow.IsChecked == true)
            {
                return txtParolaGoster.Text;
            }
            return txtParola.Password;
        }

        private void TxtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_parolaKontrolu) return;
            _parolaKontrolu = true;
            txtParolaGoster.Text = txtParola.Password;
            _parolaKontrolu = false;
        }

        private void TxtPasswordVisible_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_parolaKontrolu) return;
            _parolaKontrolu = true;
            txtParola.Password = txtParolaGoster.Text;
            _parolaKontrolu = false;
        }

        private void BtnToggleShow_Click(object sender, RoutedEventArgs e)
        {
            if (btnToggleShow.IsChecked == true)
            {
                txtParolaGoster.Visibility = Visibility.Visible;
                txtParola.Visibility = Visibility.Collapsed;
                txtParolaGoster.Focus();
                txtParolaGoster.SelectionStart = txtParolaGoster.Text.Length;
            }
            else
            {
                txtParola.Visibility = Visibility.Visible;
                txtParolaGoster.Visibility = Visibility.Collapsed;
                txtParola.Focus();
            }
        }

        private void Verify_Click(object sender, RoutedEventArgs e)
        {
            panelError.Visibility = Visibility.Collapsed;
            string girilenParola = GetParola();

            if (girilenParola == _dogruParola)
            {
                this.DialogResult = true;
                this.Close();
            }
            else
            {
                panelError.Visibility = Visibility.Visible;
                txtParola.Clear();
                txtParolaGoster.Clear();
                txtParola.Focus();
                
                var titresim = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = 0,
                    To = 10,
                    Duration = TimeSpan.FromMilliseconds(50),
                    AutoReverse = true,
                    RepeatBehavior = new System.Windows.Media.Animation.RepeatBehavior(3)
                };
                shakeTransform.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, titresim);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}