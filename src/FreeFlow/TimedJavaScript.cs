using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace FreeFlow
{
    internal abstract partial class BankScraperDriver
    {
        internal class TimedJavaScript: TaskCompletionSource<string>
        {
            private string m_TheJS;
            private bool m_bRequested = false, m_bGot = false;
            private BankScraperDriver m_Driver;
            private Predicate<string> m_Condition;

            //Errors out from worker threads - careful with continuations
            public TimedJavaScript(string TheJS, BankScraperDriver Driver, int Period, Predicate<string> Condition)
            {
                m_TheJS = TheJS;
                m_Driver = Driver;
                if (Condition == null)
                    Condition = s => !string.IsNullOrEmpty(s) && !"false".Equals(s);
                m_Condition = Condition;
                TimeSpan ts = TimeSpan.FromMilliseconds(Period);
                Device.StartTimer(ts, Request);
            }

            private bool Request()
            {
                if (!m_bRequested && !m_bGot)
                {
                    //System.Diagnostics.Debug.WriteLine("Poll req for " + (m_TheJS.Length > 10 ? m_TheJS.Substring(10) : m_TheJS));
                    m_bRequested = true;
                    m_Driver.RunJS(m_TheJS).ContinueWith(OnJSResult);
                }
                return !m_bGot;
            }

            //Sometimes the result comes twice. Shouldn't happen but does.
            private void OnJSResult(Task<string> ts)
            {
                m_bRequested = false;
                System.Diagnostics.Debug.WriteLine("Poll :" + ts.Result ?? "(null)");

                if (ts.IsFaulted && !m_bGot)
                {
                    m_bGot = true;
                    SetException(ts.Exception);
                }
                else if (m_Condition(ts.Result) && !m_bGot)
                {
                    m_bGot = true;
                    SetResult(ts.Result);
                }
                //Else the condition is not met, continue timed polling
            }
        }
    }
}
