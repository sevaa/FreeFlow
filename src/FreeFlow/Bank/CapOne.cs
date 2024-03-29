﻿using System;
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
                    DisplayMessage("Warming up the engines...");
            }

            protected override void OnNavigating(object sender, WebNavigatingEventArgs e)
            {
                base.OnNavigating(sender, e);

                if (e.Url.Equals("https://myaccounts.capitalone.com/#/welcome"))
                {
                    DisplayMessage("Waiting for the account list...");
                    PollForJavaScript(
                        "var f = function(){" +
                            "if('_FF_' in window) return true;" +
                            "window._FF_ = {};" +
                            "var oopen = window.XMLHttpRequest.prototype.open;" +
                            "window.XMLHttpRequest.prototype.open = function(){" +
                                "if(!('_FF_url' in this) && arguments[1].substr(0, 14)=='/ease-app-web/'){" +
                                    "this._FF_url = arguments[1];" +
                                    "this.addEventListener('load', function(a){" +
                                        "var url = this._FF_url;" +
                                        "if(url.substr(0, 33) == '/ease-app-web/edge/Bank/accounts/' && url.substr(url.length - 13) == '/transactions')" +
                                            "window._FF_trans = {s: this.responseText};" +
                                        "else if(url == '/ease-app-web/customer/accountsummary')" +
                                            "window._FF_accounts = {s: this.responseText};" +
                                    "});" +
                                "}" +
                                "oopen.apply(this, arguments);" +
                            "};" +
                            "return false;" +
                        "}; f()").ContinueWith(OnHooked, TaskScheduler.FromCurrentSynchronizationContext());
                }
            }

            //Hooked the XHR, wait for the account list to come
            private async void OnHooked(Task<string> ts)
            {
                string s = await PollForJavaScript("window._FF_accounts", 10);
                base.ListAccounts(JSON.Parse<ScrapedAccounts>(Unwrap(s)).accounts);
            }

            //Navigated to the logon page - no credential saving for now.
            private void Login()
            {
                /*
                string[] Credentials = await GetCredentials();
                if (!string.IsNullOrEmpty(Credentials[0]) && !string.IsNullOrEmpty(Credentials[1]))
                    RunJS("new function(){var e = document.getElementById(\"no-acct-uid\"); e.value=\"" + JSEncode(Credentials[0]) + "\"; var evt = new Event(\"change\"); e.dispatchEvent(evt); e = document.getElementById(\"no-acct-pw\"); e.value=\"" + JSEncode(Credentials[1]) + "\"; evt = new Event(\"change\"); e.dispatchEvent(evt);document.getElementById(\"no-acct-submit\").click();}()").Start();
                */
                DisplayMessage("Please sign in to your account(s).");
            }

            override async protected void ProceedToStatement(IScrapedAccount Acct)
            {
                DisplayMessage("Waiting for " + Acct.Nickname + "...");

                string id = (Acct as ScrapedAccount).referenceId;

                //Wait for the account block to click on
                await PollForJavaScript("var f = function(){var e = document.getElementById('account-" + id + "');" +
                    "if(e != null)e.click();" +
                    "return e != null;}; f()");

                //Wait for transactions
                string s = await PollForJavaScript("window._FF_trans");
                ScrapedXacts Statement = JSON.Parse<ScrapedXacts>(Unwrap(s));
                Xact[] Pending = Statement.pending == null ? new Xact[0] : Statement.pending.Select(ToXact).ToArray(),
                    Posted = Statement.posted == null ? new Xact[0] : Statement.posted.Select(ToXact).ToArray();
                if (Posted.Length > 0 && Pending.Length > 0)
                    Account.RunBalanceForward(Posted[0].Balance, Pending);

                m_Account.OnDownloadedTransactions(Pending.ArrayConcat(Posted));
                Task _t = Navigation.PopModalAsync();
            }

            //Passing strings via RunJS JSON-encodes them. Passing objects works fine.
            private static string Unwrap(string s)
            {
                return JSON.Parse<Wrapper>(s).s;
            }

            //When the XHR hook catches the transactions REST call

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

            #region Capital One REST API data elements

            [DataContract]
            class ScrapedXact
            {
                public ScrapedXact() { }

                [DataContract]
                public class Overview
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

            [DataContract]
            class ScrapedXacts
            {
                public ScrapedXacts() { }

                [DataMember] public ScrapedXact[] pending { get; set; }
                [DataMember] public ScrapedXact[] posted { get; set; }
            }

            [DataContract]
            class ScrapedAccount : IScrapedAccount
            {
                public ScrapedAccount() { }
                [DataMember] public string displayName { get; set; }
                [DataMember] public string referenceId { get; set; }
                [DataMember] public string accountNumberTLNPI { get; set; }

                public string Nickname => displayName + " (…" + accountNumberTLNPI.Substring(accountNumberTLNPI.Length - 4) + ")";
                public string AccountNumber => accountNumberTLNPI;
            }

            [DataContract]
            class ScrapedAccounts
            {
                public ScrapedAccounts() { }

                [DataMember] public ScrapedAccount[] accounts { get; set; }
            }

            [DataContract]
            private class Wrapper
            {
                public Wrapper() { }

                [DataMember] public string s { get; set; }
            }
            #endregion
        }

        public override BankScraperDriver GetScraperDriver(BankScraperPage ScraperPage, Account Acct)
        {
            return new Driver(ScraperPage, Acct);
        }
    }
}
