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
            //For balance:

            //https://myaccounts.capitalone.com/ease-app-web/edge/Bank/accountdetail/getaccountbyid/K9F4%2F%2B8WVcIOpnRL6Dnr0qwUYUQYGrzYiKwsTV6L%2Fm8%3D?productId=IM285&productType=DDA
            //For savings: https://myaccounts.capitalone.com/ease-app-web/edge/Bank/accountdetail/getaccountbyid/0wdAXQl4llJ4RLQwskUDcUKcLuD658WfvSr5pilZKfE%3D?productId=IM223&productType=SA


            //https://myaccounts.capitalone.com/ease-app-web/edge/Bank/accounts/K9F4%2F%2B8WVcIOpnRL6Dnr0qwUYUQYGrzYiKwsTV6L%2Fm8%3D/transactions
            //URLEncoded ID: K9F4%2F%2B8WVcIOpnRL6Dnr0qwUYUQYGrzYiKwsTV6L%2Fm8%3D
            //ID: account-K9F4/+8WVcIOpnRL6Dnr0qwUYUQYGrzYiKwsTV6L/m8=

            //K9F4%252F+8WVcIOpnRL6Dnr0qwUYUQYGrzYiKwsTV6L%252Fm8=

            //Accounts summary:
            // /ease-app-web/customer/accountsummary


            //TODO: after a failed login with cached credentials, don't retry with the same
            private AccountReference m_Account; //If null, we're trying to connect to an account for the first time

            public Driver(BankScraperPage ScraperPage, Account Acct = null)
                :base(Banks.CapitalOne, ScraperPage)
            {
                m_Account = Acct;
            }

            override public string InitialURL()
            {
                return "https://www.capitalone.com/?id=bank&bank=ccb";
            }

            override protected void OnNavigated(object sender, WebNavigatedEventArgs e)
            {
                System.Diagnostics.Debug.WriteLine("Nav to " + e.Url);
                if (e.Result == WebNavigationResult.Success && e.Url.Equals("https://www.capitalone.com/?id=bank&bank=ccb"))
                    Login();
                else if (e.Result == WebNavigationResult.Success && e.Url.Equals("https://myaccounts.capitalone.com/#/welcome"))
                    m_ScraperPage.MessageOff();
            }

            protected override async void OnNavigating(object sender, WebNavigatingEventArgs e)
            {
                base.OnNavigating(sender, e);

                if (e.Url.Equals("https://myaccounts.capitalone.com/#/welcome"))
                    PollForJavaScript(
                        "var f = function(){" +
                            "if('_FF_' in window && 'o' in window._FF_)return true;"+
                            "if(!('_FF_' in window))window._FF_ = {l:'Started '};" +
                            "var oopen = window.XMLHttpRequest.prototype.open;" +
                            "window.XMLHttpRequest.prototype.open = function(){" +
                                "window._FF_.o = true;" +
                                "if(!('_FF_url' in this)){"+
                                    "this._FF_url = arguments[1];" +
                                    "this.addEventListener('load', function(a){" +
                                        "var url = this._FF_url;" +
                                        //"window._FF_.l += url + ' '"+
                                        "if(url.substr(0, 33) == '/ease-app-web/edge/Bank/accounts/' && url.substr(url.length - 13) == '/transactions')" +
                                            "window._FF_trans = this.responseText;" +
                                        //"else if(url.substr(0, 53) =='/ease-app-web/edge/Bank/accountdetail/getaccountbyid/')" +
                                            //"window._FF_summary = this.responseText;" +
                                        "else if(url == '/ease-app-web/customer/accountsummary')" +
                                            "window._FF_accounts = this.responseText;" +

                                    "});"+
                                "}" +
                                "oopen.apply(this, arguments);" +
                            "};"+
                            "return false;" +
                        "}; f()", OnHooked, 10);
            }

            private void OnHooked(string s)
            {
                PollForJavaScript("window._FF_accounts", ListAccounts, 10);
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

            private async void ListAccounts(string s)
            {
                //There's a double-json bug somewhere in there...
                s = s.Replace("\\n", "\n").Replace("\\\"", "\"");

                WebAccount[] Accounts = JSON.Parse<WebAccounts>(s).accounts;

                if (Accounts == null || Accounts.Length == 0)
                {
                    m_ScraperPage.DisplayMessage("No accounts found, sorry. Probably app/website miscommunication, please contact support.");
                    return;
                }

                if (m_Account == null) //Trying to connect to an account
                {
                    if(Accounts.Length == 1) //Exactly one
                    {
                        OnAccountSelection(new AccountReference()
                        {
                            Nickname = Accounts[0].Nickname,
                            Code = Accounts[0].accountNumberTLNPI,
                            Bank = Banks.CapitalOne,
                            ExtraData = Accounts[0]
                        });
                    }
                    else
                        Navigation.PushModalAsync(
                            new AccountSelectionPage(
                                Accounts.Select(wa => new AccountReference()
                                {
                                    Nickname = wa.Nickname,
                                    Code = wa.accountNumberTLNPI,
                                    Bank = Banks.CapitalOne,
                                    ExtraData = wa
                                }), OnAccountSelection));
                }
                else //Getting a statement for an existing account
                {
                    WebAccount TheAcct = Accounts.FirstOrDefault(wa => wa.accountNumberTLNPI == m_AccountRef.Code);
                    if (TheAcct == null)
                        m_ScraperPage.DisplayMessage("The selected account was not found on the page. This could be a bank/scraper miscommunication, please contact support.");
                    else
                        ProceedToStatement(TheAcct.referenceId);
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
                    m_Account = (App.Current as App).RegisterAccount(Ref);
                    ProceedToStatement(TheAcct.referenceId);
                }
            }

            private void ProceedToStatement(string id)
            {
                RunJS("document.getElementById(\"account-" + id + "\").click()");
                PollForJavaScript("window._FF_trans", OnTransactions, 10);
            }

            #region Capital One REST API data elements

            [DataContract] class WebXact
            {
                public WebXact() { }

                [DataContract] public class Overview
                {
                    public Overview() { }
                    [DataMember] public string transactionDate { get; set; }
                    [DataMember] public string transactionTitle { get; set; }
                }

                [DataMember] public string statementDescription { get; set; }
                [DataMember] public decimal transactionTotalAmount { get; set; }
                [DataMember] public Overview transactionOverview { get; set; }
                public enum XactType { Debit, Credit};
                [DataMember] public XactType debitCardType { get; set; }
            }

            [DataContract] class WebXacts
            {
                public WebXacts() { }

                [DataMember] public WebXact [] posted { get; set; }
            }

            [DataContract]
            class WebAccount
            {
                public WebAccount() { }
                [DataMember] public string displayName { get; set; }
                [DataMember] public string referenceId { get; set; }
                [DataMember] public string accountNumberTLNPI { get; set; }

                public string Nickname => displayName + " (..." + accountNumberTLNPI.Substring(accountNumberTLNPI.Length - 3) + ")";
            }

            [DataContract]
            class WebAccounts
            {
                public WebAccounts() { }

                [DataMember] public WebAccount[] accounts { get; set; }
            }
            #endregion

            private void OnTransactions(string s)
            {
                s = s.Replace("\\n", "\n").Replace("\\\"", "\"");
                m_Account.OnDownloadedTransactions(JSON.Parse<WebXacts>(s).posted.Select(ToXact).ToArray());
            }
        }

        public override BankScraperDriver GetScraperDriver(BankScraperPage ScraperPage, AccountReference Ref)
        {
            return new Driver(ScraperPage, Ref);
        }
    }
}
