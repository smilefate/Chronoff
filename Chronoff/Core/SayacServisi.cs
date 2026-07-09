using System;
using System.Windows.Threading;

namespace Chronoff.Core
{
    #region Merkezi Zamanlayıcı Sayaç Servisi (Central Countdown Timer Service)

    public class SayacServisi
    {
        private static readonly SayacServisi _instance = new SayacServisi();
        public static SayacServisi Instance => _instance;

        private readonly DispatcherTimer _timer;
        private int _kalanSure;

        public event Action<int>? Tick;
        public event Action? Finished;

        private SayacServisi()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
        }

        public bool IsEnabled => _timer.IsEnabled;

        public int KalanSure
        {
            get => _kalanSure;
            set => _kalanSure = value;
        }

        public void Start(int saniye)
        {
            _kalanSure = saniye;
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
        }

        public void Ertele(int dakika)
        {
            _kalanSure += dakika * 60;
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (_kalanSure > 0)
            {
                _kalanSure--;
                Tick?.Invoke(_kalanSure);
            }
            else
            {
                _timer.Stop();
                Finished?.Invoke();
            }
        }
    }

    #endregion
}
