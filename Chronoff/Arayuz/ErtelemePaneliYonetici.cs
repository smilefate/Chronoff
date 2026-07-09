using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Chronoff.Servis;
using TextBox = System.Windows.Controls.TextBox;
using Button = System.Windows.Controls.Button;
using ComboBox = System.Windows.Controls.ComboBox;

namespace Chronoff
{
    public static class ErtelemePaneliYonetici
    {
        /// <summary>TR: Erteleme seçeneklerinin dinamik arayüzünü oluşturur. EN: Dynamically builds the snooze options UI.</summary>
        public static void ErtelemeListesiOlustur(Arayuz arayuz)
        {
            if (arayuz.pnlErteleme == null) return;

            foreach (UIElement altOge in arayuz.pnlErteleme.Children)
            {
                if (altOge is Grid satirPaneli)
                {
                    foreach (UIElement eleman in satirPaneli.Children)
                    {
                        if (eleman is TextBox girdiKutusu)
                        {
                            girdiKutusu.PreviewTextInput -= arayuz.SadeceRakamGirisi;
                            if (girdiKutusu.Tag is OlayTutucu tutucu)
                            {
                                if (tutucu.TextChanged != null)
                                    girdiKutusu.TextChanged -= tutucu.TextChanged;
                                if (tutucu.LostFocus != null)
                                    girdiKutusu.LostFocus -= tutucu.LostFocus;
                            }
                            girdiKutusu.Tag = null;
                        }
                        else if (eleman is ComboBox secimKutusu)
                        {
                            if (secimKutusu.Tag is OlayTutucu tutucu && tutucu.SelectionChanged != null)
                            {
                                secimKutusu.SelectionChanged -= tutucu.SelectionChanged;
                            }
                            secimKutusu.Tag = null;
                        }
                        else if (eleman is Button silButonu)
                        {
                            if (silButonu.Tag is OlayTutucu tutucu && tutucu.Click != null)
                            {
                                silButonu.Click -= tutucu.Click;
                            }
                            silButonu.Tag = null;
                        }
                    }
                }
            }

            arayuz.pnlErteleme.Children.Clear();

            string secenekMetni = DilServisi.AktifDil.GetValueOrDefault("Settings.SnoozeOptionLabel", "{0}. Seçenek");
            string dakikaBirimi = DilServisi.AktifDil.GetValueOrDefault("Time.Minute", "Dakika");
            string saatBirimi = DilServisi.AktifDil.GetValueOrDefault("Time.Hour", "Saat");

            for (int i = 0; i < arayuz._ertelemeSecenekleri.Count; i++)
            {
                var ertelemeOgesi = arayuz._ertelemeSecenekleri[i];
                int indeks = i;

                Grid satirPaneli = new Grid { Margin = new Thickness(0, 0, 0, 8) };
                satirPaneli.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
                satirPaneli.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
                satirPaneli.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
                satirPaneli.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                satirPaneli.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });

                TextBlock etiket = new TextBlock
                {
                    Text = string.Format(secenekMetni, indeks + 1),
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 13
                };
                etiket.SetResourceReference(TextBlock.ForegroundProperty, "TextLight");
                Grid.SetColumn(etiket, 0);
                satirPaneli.Children.Add(etiket);

                TextBox girdiKutusu = new TextBox
                {
                    Text = ertelemeOgesi.Value.ToString(),
                    Width = 60,
                    Height = 28,
                    BorderThickness = new Thickness(1),
                    TextAlignment = TextAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    FontSize = 12,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Left
                };
                girdiKutusu.SetResourceReference(System.Windows.Controls.Control.BackgroundProperty, "SurfaceContainer");
                girdiKutusu.SetResourceReference(System.Windows.Controls.Control.ForegroundProperty, "TextLight");
                girdiKutusu.SetResourceReference(System.Windows.Controls.Control.BorderBrushProperty, "BorderColor");
                
                var girdiOlayTutucu = new OlayTutucu();
                girdiKutusu.PreviewTextInput += arayuz.SadeceRakamGirisi;
                
                TextChangedEventHandler txtDegisti = (s, e) =>
                {
                    if (int.TryParse(girdiKutusu.Text, out int sayi))
                    {
                        int ustSinir = (ertelemeOgesi.Unit == "Saat" || ertelemeOgesi.Unit == "Hour") ? 5 : 59;
                        if (sayi > ustSinir)
                        {
                            girdiKutusu.Text = ustSinir.ToString();
                            girdiKutusu.SelectionStart = girdiKutusu.Text.Length;
                            sayi = ustSinir;
                        }
                        else if (sayi < 1)
                        {
                            girdiKutusu.Text = "1";
                            girdiKutusu.SelectionStart = girdiKutusu.Text.Length;
                            sayi = 1;
                        }
                        ertelemeOgesi.Value = sayi;
                    }
                    else
                    {
                        ertelemeOgesi.Value = 1;
                    }
                    if (arayuz._ayarlarYuklendi) arayuz.AyarlariKaydet();
                };
                girdiKutusu.TextChanged += txtDegisti;
                girdiOlayTutucu.TextChanged = txtDegisti;
 
                RoutedEventHandler odakKaybi = (s, e) =>
                {
                    if (!int.TryParse(girdiKutusu.Text, out int sayi) || sayi <= 0)
                    {
                        girdiKutusu.Text = "1";
                        ertelemeOgesi.Value = 1;
                    }
                    if (arayuz._ayarlarYuklendi) arayuz.AyarlariKaydet();
                };
                girdiKutusu.LostFocus += odakKaybi;
                girdiOlayTutucu.LostFocus = odakKaybi;
                
                girdiKutusu.Tag = girdiOlayTutucu;
 
                Grid.SetColumn(girdiKutusu, 1);
                satirPaneli.Children.Add(girdiKutusu);
 
                ComboBox secimKutusu = new ComboBox
                {
                    Style = (Style)arayuz.FindResource("FluentComboBox"),
                    Width = 90,
                    Height = 28,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Left
                };
                secimKutusu.Items.Add(dakikaBirimi);
                secimKutusu.Items.Add(saatBirimi);
                secimKutusu.SelectedItem = (ertelemeOgesi.Unit == "Saat" || ertelemeOgesi.Unit == "Hour") ? saatBirimi : dakikaBirimi;
 
                var secimOlayTutucu = new OlayTutucu();
                SelectionChangedEventHandler secimDegisti = (s, e) =>
                {
                    if (secimKutusu.SelectedItem != null)
                    {
                        string secilenMetin = secimKutusu.SelectedItem.ToString()!;
                        string yeniBirim = (secilenMetin == saatBirimi) ? "Saat" : "Dakika";
                        ertelemeOgesi.Unit = yeniBirim;
                        int ustSinir = yeniBirim == "Saat" ? 5 : 59;
                        if (ertelemeOgesi.Value > ustSinir)
                        {
                            ertelemeOgesi.Value = ustSinir;
                            girdiKutusu.Text = ustSinir.ToString();
                        }
                        if (arayuz._ayarlarYuklendi) arayuz.AyarlariKaydet();
                    }
                };
                secimKutusu.SelectionChanged += secimDegisti;
                secimOlayTutucu.SelectionChanged = secimDegisti;
                secimKutusu.Tag = secimOlayTutucu;

                Grid.SetColumn(secimKutusu, 2);
                satirPaneli.Children.Add(secimKutusu);

                Button silButonu = new Button
                {
                    Content = "✕",
                    Style = (Style)arayuz.FindResource("IncrementButtonStyle"),
                    Width = 28,
                    Height = 28,
                    Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFBA1A1A")),
                    FontWeight = FontWeights.Bold,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                    IsEnabled = arayuz._ertelemeSecenekleri.Count > 1
                };

                var silOlayTutucu = new OlayTutucu();
                RoutedEventHandler tiklama = (s, e) =>
                {
                    arayuz._ertelemeSecenekleri.RemoveAt(indeks);
                    arayuz.ErtelemeListesi();
                    if (arayuz._ayarlarYuklendi) arayuz.AyarlariKaydet();
                };
                silButonu.Click += tiklama;
                silOlayTutucu.Click = tiklama;
                silButonu.Tag = silOlayTutucu;

                Grid.SetColumn(silButonu, 4);
                satirPaneli.Children.Add(silButonu);

                arayuz.pnlErteleme.Children.Add(satirPaneli);
            }

            if (arayuz.btnErtelemeSecenegiEkle != null)
            {
                arayuz.btnErtelemeSecenegiEkle.IsEnabled = arayuz._ertelemeSecenekleri.Count < 4;
            }
        }
    }

    internal class OlayTutucu
    {
        public TextChangedEventHandler? TextChanged { get; set; }
        public RoutedEventHandler? LostFocus { get; set; }
        public SelectionChangedEventHandler? SelectionChanged { get; set; }
        public RoutedEventHandler? Click { get; set; }
    }
}
