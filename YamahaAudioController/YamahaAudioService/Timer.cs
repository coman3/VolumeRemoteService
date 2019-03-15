using System;

namespace YamahaAudioService
{
    /// <summary>
    /// Custom Timer class, that is actually a wrapper over System.Threading.Timer
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    internal class CustomTimer : IDisposable
    {
        System.Threading.Timer _timer;

        public CustomTimer()
        {

        }
        public CustomTimer(int interval) : this()
        {
            this.Interval = interval;
        }

        public bool AutoReset { get; set; }
        public bool Enabled => _timer != null;
        public int Interval { get; set; }
        public Action<object> OnTimer { get; internal set; }

        public void Dispose()
        {
            if (_timer != null)
            {
                _timer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
                _timer.Dispose();
                _timer = null;
            }
        }

        public void Start()
        {
            _timer = new System.Threading.Timer(
                new System.Threading.TimerCallback(OnTimer), null, Interval, System.Threading.Timeout.Infinite);
        }
        public void Stop()
        {
            if (_timer != null)
            {
                _timer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
            }
        }
    }
}
