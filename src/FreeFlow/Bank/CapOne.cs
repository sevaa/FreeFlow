using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace FreeFlow.Bank
{
    class CapOne: BankConnection
    {
        private class Driver: BankScraperDriver
        {
            bool m_bRequestedAcctList = false, m_bGotAcctList = false;

            public Driver(BankScraperPage ScraperPage, ScraperMode Mode)
                :base(Banks.CapitalOne, ScraperPage, Mode)
            {
            }

            override public string InitialURL()
            {
                return "https://www.capitalone.com/?id=bank&bank=ccb";
            }

            override protected void OnNavigated(object sender, WebNavigatedEventArgs e)
            {
                if (e.Result == WebNavigationResult.Success && e.Url.Equals("https://www.capitalone.com/?id=bank&bank=ccb"))
                    Login();
                else if (e.Result == WebNavigationResult.Success && e.Url.Equals("https://myaccounts.capitalone.com/#/welcome"))
                {
                    m_ScraperPage.MessageOff();
                    Device.StartTimer(TimeSpan.FromMilliseconds(100), WatchForAccounts);
                }
                else if (e.Result == WebNavigationResult.Success && e.Url.Equals("https://myaccounts.capitalone.com/accountSummary"))
                    ListAccounts();
            }

            private async void Login()
            {
                //Do we have cached credentials?
                string[] Credentials = await GetCredentials();
                if (!string.IsNullOrEmpty(Credentials[0]) && !string.IsNullOrEmpty(Credentials[1]))
                    RunJS("new function(){var e = document.getElementById(\"no-acct-uid\"); e.value=\"" + JSEncode(Credentials[0]) + "\"; var evt = new Event(\"change\"); e.dispatchEvent(evt); e = document.getElementById(\"no-acct-pw\"); e.value=\"" + JSEncode(Credentials[1]) + "\"; evt = new Event(\"change\"); e.dispatchEvent(evt);document.getElementById(\"no-acct-submit\").click();}()").Start();
                else if ((App.Current as App).ManualLogin(m_Bank))
                    m_ScraperPage.DisplayMessage("Please sign in to your account(s).");
            }

            private bool WatchForAccounts()
            {
                if (!m_bRequestedAcctList && !m_bGotAcctList)
                {
                    m_bRequestedAcctList = true;
                    RunJS("document.getElementById(\"summaryParent\") != null").ContinueWith(OnAcctListProbe);
                }
                return !m_bGotAcctList;
            }

            private void OnAcctListProbe(Task<string> ts)
            {
                m_bRequestedAcctList = false;
                if (ts.IsCompleted && ts.Result == "true")
                {
                    m_bGotAcctList = true;
                    Device.BeginInvokeOnMainThread(ListAccounts);
                }
            }

            //Need ID, or would've used 
            [DataContract]class WebAccount : AccountReference
            {
                public WebAccount() { }

                [DataMember] public string id { get; set; }
                [DataMember] public string no { get; set; }
                [DataMember] public string title { get; set; }

                override public string ToString()
                {
                    return title + " - " + no.Substring(3);
                }
            }

            private async void ListAccounts()
            {
                string s = await RunJS("new function(){return JSON.stringify([].slice.call(document.getElementById(\"summaryParent\").children,0).map(function(li){return {id:li.id, title:li.getElementsByTagName(\"h2\").item(0).innerText, no:li.getElementsByClassName(\"accnumbertrail\").item(0).innerText};}));}();");
                List<WebAccount> WebAccounts;
                using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(s)))
                    WebAccounts = new DataContractJsonSerializer(typeof(List<WebAccount>)).ReadObject(ms) as List<WebAccount>;

                if(WebAccounts == null || WebAccounts.Count == 0)
                {
                    m_ScraperPage.DisplayMessage("No accounts found, sorry. Probably app/website miscommunication, please contact support.");
                    return;
                }

                if (m_Mode == ScraperMode.Connect)
                {
                    //TODO: if a lot of accounts, inconvenient
                    string[] AcctNames = WebAccounts.Select(wa => wa.ToString()).ToArray();
                    string ActionResult = await m_ScraperPage.DisplayActionSheet("Select an account", "Cancel", null, AcctNames);
                    if (ActionResult == "Cancel")
                        m_ScraperPage.Navigation.PopModalAsync();
                    else
                    {
                        int i = Array.FindIndex(AcctNames, an => an == ActionResult);
                        WebAccount TheWebAcct = WebAccounts[i];
                        (App.Current as App).RegisterAccount(m_Bank, TheWebAcct.title, TheWebAcct.no.Substring(3));
                    }
                }
                else if(m_Mode == ScraperMode.GetStatement)
                {

                }



            }
        }

        public override BankScraperDriver GetScraperDriver(BankScraperPage ScraperPage, ScraperMode Mode)
        {
            return new Driver(ScraperPage, Mode);
        }
    }
}
