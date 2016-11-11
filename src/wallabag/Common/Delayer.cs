using System;
using Windows.UI.Xaml;

// Credits go to Rudy Huyn: http://www.rudyhuyn.com/blog/2016/03/01/delay-an-action-debounce-and-throttle/
namespace wallabag.Common
{
    public class Delayer
    {
        private DispatcherTimer _timer;
        public Delayer(TimeSpan timeSpan)
        {
            _timer = new DispatcherTimer() { Interval = timeSpan };
            _timer.Tick += Timer_Tick;
        }

        public Delayer()
        {
        }

        public event RoutedEventHandler Action;

        private void Timer_Tick(object sender, object e)
        {
            _timer.Stop();
            Action?.Invoke(this, new RoutedEventArgs());
        }

        public void ResetAndTick()
        {
            _timer.Stop();
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
        }
    }
}
