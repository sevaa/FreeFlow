using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace FreeFlow
{
    interface IScrapedAccount
    {
        string AccountNumber { get; }
        string Nickname { get; }
    }


    abstract internal partial class BankScraperDriver
    {
        protected Banks m_Bank;
        protected BankScraperPage m_ScraperPage;
        protected Account m_Account; //If null, we're trying to connect to an account for the first time

        public Account Account => m_Account;

        protected BankScraperDriver(Banks Bank, BankScraperPage ScraperPage, Account Acct = null)
        {
            m_Bank = Bank;
            m_Account = Acct;
            m_ScraperPage = ScraperPage;
            m_ScraperPage.AddNavigatedHandler(OnNavigated);
            m_ScraperPage.AddNavigatingHandler(OnNavigating);
        }

        abstract public string InitialURL { get; }

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

        //TODO. Not sure we want it in the first place :(
        protected async Task<string[]> GetCredentials()
        {
            string BankName = Enum.GetName(typeof(Banks), m_Bank);

            string Username = await SecureStorage.GetAsync(BankName + "_Username");
            string Password = await SecureStorage.GetAsync(BankName + "_Password");
            return new string[] { Username, Password };
        }

        protected void PollForJavaScript(string TheScript, Action<string> OnDone, int Period = 10, Predicate<string> Condition = null)
        {
            new TimedJavaScript(TheScript, OnDone, this, Period, Condition);
        }

        #region Account selection - assuming we land on a list of accounts page

        protected void ListAccounts(IScrapedAccount []ScrapedAccounts)
        {
            if (ScrapedAccounts == null || ScrapedAccounts.Length == 0)
            {
                m_ScraperPage.DisplayMessage("No accounts found, sorry. Probably app/website miscommunication, please contact support.");
                return;
            }

            if (m_Account == null) //The scraper is trying to connect to an account
            {
                if (ScrapedAccounts.Length == 1) //Exactly one
                    OnAccountSelection(ToAccountReference(ScrapedAccounts[0]));
                else
                {
                    Task t = Navigation.PushModalAsync(new AccountSelectionPage(ScrapedAccounts.Select(ToAccountReference), OnAccountSelection));
                }
            }
            else //Getting a statement for an existing account
            {
                IScrapedAccount TheAcct = ScrapedAccounts.FirstOrDefault(sa => sa.AccountNumber == m_Account.Ref.AccountNumber);
                if (TheAcct == null)
                    m_ScraperPage.DisplayMessage("The selected account was not found on the page. This could be a bank/scraper miscommunication, please contact support.");
                else
                    ProceedToStatement(TheAcct);
            }
        }

        protected AccountReference ToAccountReference(IScrapedAccount ScrapedAcct)
        {
            return new AccountReference()
            {
                Bank = m_Bank,
                AccountNumber = ScrapedAcct.AccountNumber,
                Nickname = ScrapedAcct.Nickname,
                ExtraData = ScrapedAcct
            };
        }

        protected void OnAccountSelection(AccountReference Ref)
        {
            if (Ref == null) //Cancel
                Navigation.PopModalAsync();
            else
            {
                IScrapedAccount TheAcct = Ref.ExtraData as IScrapedAccount;
                m_Account = (App.Current as App).RegisterAccount(Ref);
                ProceedToStatement(TheAcct);
            }
        }

        abstract protected void ProceedToStatement(IScrapedAccount Acct);
        #endregion
    }
}
