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
            //TODO: after a failed login with cached credentials, don't retry with the same
            private decimal m_Balance;
            private AccountReference m_AccountRef;

            public Driver(BankScraperPage ScraperPage, ScraperMode Mode, AccountReference Ref = null)
                :base(Banks.CapitalOne, ScraperPage, Mode)
            {
                m_AccountRef = Ref;
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
                    PollForJavaScript("document.getElementById(\"summaryParent\") != null", ListAccounts);
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

            //Need ID, or would've used 
            [DataContract]class WebAccount
            {
                public WebAccount() {}

                [DataMember] public string id { get; set; }
                [DataMember] public string no { get; set; }
                [DataMember] public string title { get; set; }
                [DataMember] public string balance { get; set; }

                override public string ToString()
                {
                    return title + " - " + no.Substring(3);
                }
            }

            private async void ListAccounts()
            {
                string s = await RunJS("[].slice.call(document.getElementById(\"summaryParent\").children,0)"+
                    ".map(function(li){return {"+
                        "id:li.id, "+
                        "title:li.getElementsByTagName(\"h2\").item(0).innerText, "+
                        "no:li.getElementsByClassName(\"accnumbertrail\").item(0).innerText," +
                        "balance:li.getElementsByTagName(\"localized-currency\").item(0).getElementsByClassName(\"screen-reader-only\").item(0).innerText};})");

                List<WebAccount> WebAccounts;
                using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(s)))
                    WebAccounts = new DataContractJsonSerializer(typeof(List<WebAccount>)).ReadObject(ms) as List<WebAccount>;

                if (WebAccounts == null || WebAccounts.Count == 0)
                {
                    m_ScraperPage.DisplayMessage("No accounts found, sorry. Probably app/website miscommunication, please contact support.");
                    return;
                }

                if (m_Mode == ScraperMode.Connect)
                {
                    Navigation.PushModalAsync(
                        new AccountSelectionPage(
                            WebAccounts.Select(wa => new AccountReference()
                            {
                                Nickname = wa.ToString(),
                                Code = wa.ToString(),
                                Bank = Banks.CapitalOne,
                                ExtraData = wa
                            }), OnAccountSelection));
                }
                else if(m_Mode == ScraperMode.GetStatement)
                {
                    WebAccount TheAcct = WebAccounts.FirstOrDefault(wa => wa.ToString() == m_AccountRef.Code);
                    if (TheAcct == null)
                        m_ScraperPage.DisplayMessage("The selected account was not found on the page. This could be a bank/scraper miscommunication, please contact support.");
                    else
                    {
                        m_Balance = decimal.Parse(TheAcct.balance.Replace(",", "").Replace("$",""));
                        ProceedToStatement(TheAcct.id);
                    }
                }
            }

            //We know we're in connect mode. In
            private void OnAccountSelection(AccountReference Ref)
            {
                if (Ref == null) //Cancel
                    Navigation.PopModalAsync();
                else
                {
                    WebAccount TheAcct = Ref.ExtraData as WebAccount;
                    m_Balance = decimal.Parse(TheAcct.balance.Replace(",", ""));
                    m_AccountRef = Ref;
                    (App.Current as App).RegisterAccount(Ref);
                    ProceedToStatement(TheAcct.id);
                }
            }

            private void ProceedToStatement(string id)
            {
                RunJS("document.getElementById(\"" + id + "\").click()");
                PollForJavaScript("document.getElementById(\"downloadStatementTransactions\") != null", () =>
                {
                    RunJS("document.getElementById(\"downloadStatementTransactions\").click()");
                    PollForJavaScript("document.getElementsByClassName(\"bank-downloads-container\").length > 0", () =>
                    {
                        RunJS("if(!(\"XHRHooked\" in window)){"+
                            "var oopen = window.XMLHttpRequest.prototype.open;"+
                            "window.XMLHttpRequest.prototype.open = function(){"+
                                    "this.addEventListener('load', function(a){window.location.href=\"http://callback/boohoo\";});oopen.apply(this, arguments);}"+
                            "window.XHRHooked = true;};"+
                            "document.getElementById(\"ease-dropdown-filter-fileTypeSelection\").value=\"CSV (Spreadsheet, Excel, Numbers)\";" +
                            "document.getElementById(\"ease-dropdown-filter-dateTypeSelection\").value=\"90 days\";" +
                            "document.getElementById(\"buttonBeginExport\").click();");
                    });
                });
            }
        }

        public override BankScraperDriver GetScraperDriver(BankScraperPage ScraperPage, ScraperMode Mode, AccountReference Ref)
        {
            return new Driver(ScraperPage, Mode, Ref);
        }
    }
}
