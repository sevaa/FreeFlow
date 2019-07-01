using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace FreeFlow
{
    internal abstract partial class BankScraperDriver
    {
        internal class TimedJavaScript
        {
            private string m_TheJS;
            private bool m_bRequested = false, m_bGot = false;
            private Action<string> m_OnDone;
            private BankScraperDriver m_Driver;
            private Predicate<string> m_Condition;

            public TimedJavaScript(string TheJS, Action<string> OnDone, BankScraperDriver Driver, int Period, Predicate<string> Condition)
            {
                m_OnDone = OnDone;
                m_TheJS = TheJS;
                m_Driver = Driver;
                if (Condition == null)
                    Condition = s => !string.IsNullOrEmpty(s) && !"false".Equals(s);
                m_Condition = Condition;
                Device.StartTimer(TimeSpan.FromMilliseconds(Period), Request);
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
                System.Diagnostics.Debug.WriteLine("Poll :" + ts.Result ?? "(null)");

                if (ts.IsCompleted && m_Condition(ts.Result))
                {
                    m_bGot = true;
                    Device.BeginInvokeOnMainThread(() => m_OnDone(ts.Result));
                }
            }
        }
    }
}
