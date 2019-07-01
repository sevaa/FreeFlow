using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace FreeFlow.Bank
{
    class BofA: BankConnection
    {
        private class Driver : BankScraperDriver
        {
            public Driver(BankScraperPage ScraperPage, Account Acct)
                : base(Banks.BankOfAmerica, ScraperPage, Acct)
            { }

            public override string InitialURL => "https://www.bankofamerica.com/";

            protected override void OnNavigated(object sender, WebNavigatedEventArgs e)
            {
                base.OnNavigated(sender, e);
                if (e.Url.Equals("https://www.bankofamerica.com/"))
                    Login();
                else if (e.Url.StartsWith("https://secure.bankofamerica.com/myaccounts/signin/signIn.go"))
                    ListAccounts();
            }

            private void Login()
            {
                m_ScraperPage.DisplayMessage("Please sign in to your account(s).");
            }

            private async void ListAccounts()
            {
                m_ScraperPage.MessageOff();
                string s = await RunJS("fsdNavClientOptions");
                ScrapedAccount[] ScrapedAccounts = JSON.Parse<ScrapedAccounts>(s).fsdNavAccountBalances.personal;
                base.ListAccounts(ScrapedAccounts);
            }

            protected override void ProceedToStatement(IScrapedAccount Acct)
            {
                ScrapedAccount sa = Acct as ScrapedAccount;
                m_ScraperPage.BrowseTo(sa.url + "&" + sa.additionalParam);
                PollForJavaScript("document.getElementsByClassName('transaction-records').length > 0", OnTransactions);
            }

            private async void OnTransactions(string _)
            {
                string s = await RunJS("[].slice.call(document.getElementsByClassName('transaction-records').item(0).rows)" +
                    ".filter(function(r){return r.id;})" +
                    ".map(function(r){return {" +
                        "When: r.cells[0].innerText.substr(0, 10) == 'Processing' ? '' : r.cells[0].getElementsByTagName('span').item(1).innerText," +
                        "Desc: r.cells[1].getElementsByClassName('transTitleForEditDesc').length > 0 ? r.cells[1].getElementsByClassName('transTitleForEditDesc').item(0).innerText : r.cells[1].innerText," +
                        "Amount: r.cells[4].innerText," +
                        "Balance: r.cells[5].innerText"+
                    "};})");

                ScrapedXact[] Statement = JSON.Parse<List<ScrapedXact>>(s).ToArray();
                m_Account.OnDownloadedTransactions(Statement.Select(ToXact).ToArray());
                Task t = Navigation.PopModalAsync();
            }

            private Xact ToXact(ScrapedXact sx)
            {
                return new Xact()
                {
                    Amount = decimal.Parse(sx.Amount.Replace("$", "").Replace(",", "")),
                    Desc = sx.Desc.Trim(),
                    When = sx.When == string.Empty ? DateTime.Today : DateTime.ParseExact(sx.When, "MM/dd/yyyy", CultureInfo.InvariantCulture),
                    Balance = decimal.Parse(sx.Balance.Replace("$", "").Replace(",", "")),
                    IsPending = sx.When == string.Empty,
                    IsProjected = false,
                    IsRecurring = false,
                    RecurrenceID = 0
                };
            }

            #region Data items

            [DataContract] private class ScrapedAccount : IScrapedAccount
            {
                public ScrapedAccount() { }

                [DataMember] public string accountDisplayName { get; set; }
                [DataMember] public string balance { get; set; }
                [DataMember] public string url { get; set; }
                [DataMember] public string additionalParam { get; set; }

                public string AccountNumber => accountDisplayName.Substring(accountDisplayName.LastIndexOf(" - ") + 3);
                public string Nickname => accountDisplayName;
            }

            [DataContract] private class ScrapedAccounts
            {
                public ScrapedAccounts() { }

                [DataContract]public class Balances
                {
                    public Balances() { }

                    [DataMember] public ScrapedAccount [] personal { get; set; }
                }

                [DataMember] public Balances fsdNavAccountBalances { get; set; }
            }

            [DataContract] private class ScrapedXact
            {
                public ScrapedXact() { }

                [DataMember] public string Desc { get; set; }
                [DataMember] public string When { get; set; }
                [DataMember] public string Amount { get; set; }
                [DataMember] public string Balance { get; set; }
            }
            #endregion
        }

        public override BankScraperDriver GetScraperDriver(BankScraperPage ScraperPage, Account Acct)
        {
            return new Driver(ScraperPage, Acct);
        }
    }
}
