using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace FreeFlow
{
    public abstract partial class BankScraperDriver
    {
        internal class TimedJavaScript
        {
            private string m_TheJS;
            private bool m_bRequested = false, m_bGot = false;
            private Action m_OnDone;
            BankScraperDriver m_Driver;

            public TimedJavaScript(string TheJS, Action OnDone, BankScraperDriver Driver)
            {
                m_OnDone = OnDone;
                m_TheJS = TheJS;
                m_Driver = Driver;
                Device.StartTimer(TimeSpan.FromMilliseconds(100), Request);
            }

            private bool Request()
            {
                if (!m_bRequested && !m_bGot)
                {
                    m_bRequested = true;
                    m_Driver.RunJS(m_TheJS).ContinueWith(OnJSResult);
                }
                return !m_bGot;
            }

            private void OnJSResult(Task<string> ts)
            {
                m_bRequested = false;
                if (ts.IsCompleted && ts.Result == "true")
                {
                    m_bGot = true;
                    Device.BeginInvokeOnMainThread(m_OnDone);
                }
            }
        }
    }
}
