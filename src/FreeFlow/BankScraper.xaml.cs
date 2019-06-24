using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace FreeFlow
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class BankScraper : ContentPage
    {
        public BankScraper()
        {
            InitializeComponent();
            TheWeb.Source = "https://www.capitalone.com/?id=bank&bank=ccb";
            //TheWeb.Source = "https://myaccounts.capitalone.com/accountSummary";
        }

        private void OnNavigating(object sender, WebNavigatingEventArgs e)
        {

        }


        private void OnNavigated(object sender, WebNavigatedEventArgs e)
        {
            if (e.Result == WebNavigationResult.Success && e.Url.Equals("https://www.capitalone.com/?id=bank&bank=ccb"))
                Login();
            else if (e.Result == WebNavigationResult.Success && e.Url.Equals("https://myaccounts.capitalone.com/#/welcome"))
                Device.StartTimer(TimeSpan.FromMilliseconds(100), WatchForAccounts);
            else if (e.Result == WebNavigationResult.Success && e.Url.Equals("https://myaccounts.capitalone.com/accountSummary"))
                ListAccounts();

        }

        bool m_bRequestedAcctList = false, m_bGotAcctList = false;

        private bool WatchForAccounts()
        {
            if (!m_bRequestedAcctList)
            {
                m_bRequestedAcctList = true;
                TheWeb.EvaluateJavaScriptAsync("document.getElementById(\"summaryParent\") != null").ContinueWith(OnAcctListProbe);
            }
            return !m_bGotAcctList;
        }

        private void OnAcctListProbe(Task<string> ts)
        {
            if (ts.IsCompleted && ts.Result == "true")
            {
                m_bGotAcctList = true;
                ListAccounts();
            }
        }

        private void Login()
        {
            TheWeb.EvaluateJavaScriptAsync("new function(){var e = document.getElementById(\"no-acct-uid\"); e.value=\"SevaAleks\"; var evt = new Event(\"change\"); e.dispatchEvent(evt); e = document.getElementById(\"no-acct-pw\"); e.value=\"\"; evt = new Event(\"change\"); e.dispatchEvent(evt);document.getElementById(\"no-acct-submit\").click();}()");
        }

        private async void ListAccounts()
        {
            string s = await TheWeb.EvaluateJavaScriptAsync("new function(){return JSON.stringify([].slice.call(document.getElementById(\"summaryParent\").children,0).map(function(li){return {id:li.id, title:li.getElementsByTagName(\"h2\").item(0).innerText};));}();");
        }
    }
}