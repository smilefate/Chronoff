using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Chronoff.Servis;

namespace Chronoff.Core
{
    #region Bildirim ve Sistem Tepsisi (Tray) Yönetimi (Notification & System Tray Center)

    public class BildirimMerkezi
    {
        private static readonly BildirimMerkezi _instance = new BildirimMerkezi();
        public static BildirimMerkezi Instance => _instance;

        private NotifyIcon? _uygulamaSimgesi;
        private System.Drawing.Icon? _extractedIcon;
        private Action? _acmaEylemi;
        private Action? _gorevIptalEylemi;
        private Action? _cikisEylemi;
        private Func<bool>? _gorevCalisiyorMu;

        private ToolStripMenuItem? _uygulamayiAc;
        private ToolStripMenuItem? _gorevIptal;
        private ToolStripMenuItem? _uygulamayiKapat;

        private BildirimMerkezi()
        {
        }

        public void Initialize(Action acmaEylemi, Action gorevIptalEylemi, Action cikisEylemi, Func<bool> gorevCalisiyorMu)
        {
            _acmaEylemi = acmaEylemi;
            _gorevIptalEylemi = gorevIptalEylemi;
            _cikisEylemi = cikisEylemi;
            _gorevCalisiyorMu = gorevCalisiyorMu;

            _uygulamaSimgesi = new NotifyIcon
            {
                Text = "Chronoff v1.0 - Arka Planda Çalışıyor"
            };

            try
            {
                string? exeYolu = Environment.ProcessPath;

                if (!string.IsNullOrEmpty(exeYolu) && File.Exists(exeYolu))
                {
                    _extractedIcon = System.Drawing.Icon.ExtractAssociatedIcon(exeYolu);
                    _uygulamaSimgesi.Icon = _extractedIcon;
                }
                else
                {
                    _uygulamaSimgesi.Icon = System.Drawing.SystemIcons.Application;
                }
            }
            catch
            {
                _uygulamaSimgesi.Icon = System.Drawing.SystemIcons.Application;
            }

            if (_uygulamaSimgesi.Icon == null)
            {
                _uygulamaSimgesi.Icon = System.Drawing.SystemIcons.Application;
            }

            _uygulamaSimgesi.Visible = false;

            _uygulamaSimgesi.Click += UygulamaSimgesi_Tiklama;
            _uygulamaSimgesi.DoubleClick += UygulamaSimgesi_Tiklama;

            _uygulamayiAc = new ToolStripMenuItem();
            _uygulamayiAc.Click += (s, ev) => _acmaEylemi?.Invoke();

            _gorevIptal = new ToolStripMenuItem();
            _gorevIptal.Click += (s, ev) => _gorevIptalEylemi?.Invoke();

            _uygulamayiKapat = new ToolStripMenuItem();
            _uygulamayiKapat.Click += (s, ev) => _cikisEylemi?.Invoke();

            var tepsiMenusu = new ContextMenuStrip();
            tepsiMenusu.Items.Add(_uygulamayiAc);
            tepsiMenusu.Items.Add(_gorevIptal);
            tepsiMenusu.Items.Add(_uygulamayiKapat);

            tepsiMenusu.Opening += ContextMenu_Opening;
            _uygulamaSimgesi.ContextMenuStrip = tepsiMenusu;
        }

        public bool Visible
        {
            get => _uygulamaSimgesi?.Visible ?? false;
            set
            {
                if (_uygulamaSimgesi != null)
                {
                    _uygulamaSimgesi.Visible = value;
                }
            }
        }

        public string Text
        {
            get => _uygulamaSimgesi?.Text ?? "";
            set
            {
                if (_uygulamaSimgesi != null)
                {
                    _uygulamaSimgesi.Text = value;
                }
            }
        }

        public void ShowBalloonTip(int timeout, string tipTitle, string tipText, ToolTipIcon tipIcon)
        {
            _uygulamaSimgesi?.ShowBalloonTip(timeout, tipTitle, tipText, tipIcon);
        }

        public void Dispose()
        {
            if (_uygulamayiAc != null)
            {
                _uygulamayiAc.Dispose();
                _uygulamayiAc = null;
            }
            if (_gorevIptal != null)
            {
                _gorevIptal.Dispose();
                _gorevIptal = null;
            }
            if (_uygulamayiKapat != null)
            {
                _uygulamayiKapat.Dispose();
                _uygulamayiKapat = null;
            }
            if (_uygulamaSimgesi != null)
            {
                _uygulamaSimgesi.Dispose();
                _uygulamaSimgesi = null;
            }
            if (_extractedIcon != null)
            {
                _extractedIcon.Dispose();
                _extractedIcon = null;
            }
        }

        private void UygulamaSimgesi_Tiklama(object? sender, EventArgs e)
        {
            var fareOlayi = e as MouseEventArgs;
            if (fareOlayi != null && fareOlayi.Button == MouseButtons.Right)
            {
                return;
            }
            _acmaEylemi?.Invoke();
        }

        private void ContextMenu_Opening(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_uygulamayiAc == null || _gorevIptal == null || _uygulamayiKapat == null) return;

            string openText = "Uygulamayı aç";
            string cancelText = "Görevi iptal et";
            string exitText = "Kapat";

            if (DilServisi.AktifDil != null)
            {
                if (DilServisi.AktifDil.TryGetValue("SysTray.MenuOpen", out var o)) openText = o;
                if (DilServisi.AktifDil.TryGetValue("SysTray.MenuCancel", out var c)) cancelText = c;
                if (DilServisi.AktifDil.TryGetValue("SysTray.MenuExit", out var ex)) exitText = ex;
            }

            _uygulamayiAc.Text = openText;
            _gorevIptal.Text = cancelText;
            _uygulamayiKapat.Text = exitText;

            bool gorevAktifMi = _gorevCalisiyorMu != null && _gorevCalisiyorMu();
            _gorevIptal.Visible = gorevAktifMi;
        }
    }

    #endregion
}
