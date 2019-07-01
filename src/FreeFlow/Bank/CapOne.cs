using System;
using System.Collections.Generic;
using System.Globalization;
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
            public Driver(BankScraperPage ScraperPage, Account Acct = null)
                :base(Banks.CapitalOne, ScraperPage, Acct)
            {
            }

            override public string InitialURL => "https://www.capitalone.com/?id=bank&bank=ccb";

            override protected void OnNavigated(object sender, WebNavigatedEventArgs e)
            {
                //System.Diagnostics.Debug.WriteLine("Nav to " + e.Url);
                if (e.Result == WebNavigationResult.Success && e.Url.Equals("https://www.capitalone.com/?id=bank&bank=ccb"))
                    Login();
                else if (e.Result == WebNavigationResult.Success && e.Url.Equals("https://myaccounts.capitalone.com/#/welcome"))
                    m_ScraperPage.MessageOff();
            }

            protected override void OnNavigating(object sender, WebNavigatingEventArgs e)
            {
                base.OnNavigating(sender, e);

                if (e.Url.Equals("https://myaccounts.capitalone.com/#/welcome"))
                    PollForJavaScript(
                        "var f = function(){" +
                            "if('_FF_' in window && 'o' in window._FF_)return true;"+
                            "if(!('_FF_' in window))window._FF_ = {};" +
                            "var oopen = window.XMLHttpRequest.prototype.open;" +
                            "window.XMLHttpRequest.prototype.open = function(){" +
                                "window._FF_.o = true;" +
                                "if(!('_FF_url' in this) && arguments[1].substr(0, 14)=='/ease-app-web/'){" +
                                    "this._FF_url = arguments[1];" +
                                    "this.addEventListener('load', function(a){" +
                                        "var url = this._FF_url;" +
                                        "if(url.substr(0, 33) == '/ease-app-web/edge/Bank/accounts/' && url.substr(url.length - 13) == '/transactions')" +
                                            "window._FF_trans = this.responseText;" +
                                        "else if(url == '/ease-app-web/customer/accountsummary')" +
                                            "window._FF_accounts = this.responseText;" +
                                    "});"+
                                "}" +
                                "oopen.apply(this, arguments);" +
                            "};"+
                            "return false;" +
                        "}; f()", OnHooked, 10);
            }

            //Hooked the XHR, wait for the account list to come
            private void OnHooked(string s)
            {
                PollForJavaScript("window._FF_accounts", ListAccounts, 10);
            }

            //Navigated to the logon page - no credential saving for now.
            private async void Login()
            {
                string[] Credentials = await GetCredentials();
                if (!string.IsNullOrEmpty(Credentials[0]) && !string.IsNullOrEmpty(Credentials[1]))
                    RunJS("new function(){var e = document.getElementById(\"no-acct-uid\"); e.value=\"" + JSEncode(Credentials[0]) + "\"; var evt = new Event(\"change\"); e.dispatchEvent(evt); e = document.getElementById(\"no-acct-pw\"); e.value=\"" + JSEncode(Credentials[1]) + "\"; evt = new Event(\"change\"); e.dispatchEvent(evt);document.getElementById(\"no-acct-submit\").click();}()").Start();
                else if ((App.Current as App).ManualLogin(m_Bank))
                    m_ScraperPage.DisplayMessage("Please sign in to your account(s).");
            }

            private void ListAccounts(string s)
            {
                //There's a double-json bug somewhere in there...
                s = s.Replace("\\n", "\n").Replace("\\\"", "\"");
                base.ListAccounts(JSON.Parse<ScrapedAccounts>(s).accounts);
            }

            override protected void ProceedToStatement(IScrapedAccount Acct)
            {
                string id = (Acct as ScrapedAccount).referenceId;
                PollForJavaScript("var f = function(){var e = document.getElementById('account-" + id + "');" +
                    "if(e != null)e.click();"+
                    "return e != null;}; f()", s =>
                    {
                        PollForJavaScript("window._FF_trans", OnTransactions, 10);
                    });
            }

            #region Capital One REST API data elements

            [DataContract] class ScrapedXact
            {
                public ScrapedXact() { }

                [DataContract] public class Overview
                {
                    public Overview() { }
                    [DataMember] public string transactionDate { get; set; }
                    [DataMember] public string transactionTitle { get; set; }
                    [DataMember] public decimal accountBalance { get; set; }
                }

                [DataMember] public string statementDescription { get; set; }
                [DataMember] public decimal transactionTotalAmount { get; set; }
                [DataMember] public Overview transactionOverview { get; set; }
                [DataMember] public string debitCardType { get; set; } //Debit, Credit
                [DataMember] public string transactionStatus { get; set; } //pending, posted
            }

            [DataContract] class ScrapedXacts
            {
                public ScrapedXacts() { }

                [DataMember] public ScrapedXact[] pending { get; set; }
                [DataMember] public ScrapedXact [] posted { get; set; }
            }

            [DataContract]
            class ScrapedAccount : IScrapedAccount
            {
                public ScrapedAccount() { }
                [DataMember] public string displayName { get; set; }
                [DataMember] public string referenceId { get; set; }
                [DataMember] public string accountNumberTLNPI { get; set; }

                public string Nickname => displayName + " (..." + accountNumberTLNPI.Substring(accountNumberTLNPI.Length - 3) + ")";
                public string AccountNumber => accountNumberTLNPI;
            }

            [DataContract]
            class ScrapedAccounts
            {
                public ScrapedAccounts() { }

                [DataMember] public ScrapedAccount[] accounts { get; set; }
            }
            #endregion

            //When the XHR hook catches the transactions REST call
            private void OnTransactions(string s)
            {
                s = s.Replace("\\n", "\n").Replace("\\\"", "\"");
                ScrapedXacts Statement = JSON.Parse<ScrapedXacts>(s);
                Xact [] Pending = Statement.pending.Select(ToXact).ToArray(),
                    Posted = Statement.posted.Select(ToXact).ToArray();
                Account.RunBalanceForward(Posted[0].Balance, Pending);

                m_Account.OnDownloadedTransactions(Pending.ArrayConcat(Posted));
                Navigation.PopModalAsync();
            }

            private Xact ToXact(ScrapedXact sx)
            {
                return new Xact()
                {
                    Amount = sx.debitCardType == "Credit" ? sx.transactionTotalAmount : -sx.transactionTotalAmount,
                    Desc = sx.statementDescription.Trim(),
                    When = DateTime.ParseExact(sx.transactionOverview.transactionDate.Split('T')[0], "yyyy-MM-dd", CultureInfo.InvariantCulture),
                    Balance = sx.transactionOverview.accountBalance,
                    IsPending = sx.transactionStatus == "pending",
                    IsProjected = false,
                    IsRecurring = false,
                    RecurrenceID = 0
                };
            }
        }

        public override BankScraperDriver GetScraperDriver(BankScraperPage ScraperPage, Account Acct)
        {
            return new Driver(ScraperPage, Acct);
        }
    }
}
