using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace FreeFlow
{
    abstract public partial class BankScraperDriver
    {
        protected Banks m_Bank;
        protected BankScraperPage m_ScraperPage;
        protected ScraperMode m_Mode;

        protected BankScraperDriver(Banks Bank, BankScraperPage ScraperPage, ScraperMode Mode)
        {
            m_Bank = Bank;
            m_ScraperPage = ScraperPage;
            m_ScraperPage.AddNavigatedHandler(OnNavigated);
            m_ScraperPage.AddNavigatingHandler(OnNavigating);
            m_Mode = Mode;
        }

        abstract public string InitialURL();

        protected virtual void OnNavigated(object sender, WebNavigatedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("--- Navigated to " + e.Url);
        }

        protected virtual void OnNavigating(object sender, WebNavigatingEventArgs e)
        {
            Uri u = new Uri(e.Url);
            if(u.Host == "callback")
            {
                System.Diagnostics.Debug.WriteLine("~~~ JS Noti: " + e.Url);
                e.Cancel = true;
            }
        }

        public Task<string> RunJS(string s)
        {
            return m_ScraperPage.RunJS(s);
        }

        protected INavigation Navigation => m_ScraperPage.Navigation;

        //For single line strings
        protected string JSEncode(string s)
        {
            return s.Replace("\"", "\\\"");
        }

        protected async Task<string[]> GetCredentials()
        {
            string BankName = Enum.GetName(typeof(Banks), m_Bank);

            string Username = await SecureStorage.GetAsync(BankName + "_Username");
            string Password = await SecureStorage.GetAsync(BankName + "_Password");
            return new string[] { Username, Password };
        }

        protected void PollForJavaScript(string TheScript, Action<string> OnDone, int Period)
        {
            new TimedJavaScript(TheScript, OnDone, this, Period);
        }
    }
}
