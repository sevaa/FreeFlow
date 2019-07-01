using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace FreeFlow
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(true)]
    public partial class AccountPage : ContentPage
    {
        private Account m_Account;

        internal AccountPage(AccountReference Ref)
        {
            InitializeComponent();
            m_Account = new Account(Ref);
            Init();
        }

        internal AccountPage(Account Acct)
        {
            InitializeComponent();
            m_Account = Acct;
            Init();
        }

        private void Init()
        { 
            m_Account.Change += OnAccountChange;
            Title = m_Account.Ref.Nickname;
            TheList.ItemsSource = m_Account.Xacts();
            if (m_Account.NoRecurrences)
                DisplayMessage("Tap on recurring transactions and define their recurrences.");
        }

        private void DisplayMessage(string s)
        {
            lMessage.Text = s;
            lMessage.IsVisible = true;
        }

        private void OnAccountChange()
        {
            TheList.ItemsSource = m_Account.Xacts();
            lMessage.IsVisible = m_Account.NoRecurrences;
        }

        private void OnXactSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if(e.SelectedItem != null)
                Navigation.PushAsync(new XactDetailsPage(m_Account, e.SelectedItem as Xact), true);
        }

        private void OnRefresh(object sender, EventArgs e)
        {
            Navigation.PushModalAsync(new BankScraperPage((App.Current as App).GetBankConnection(m_Account.Ref.Bank), m_Account));
        }

        private void OnAbout(object sender, EventArgs e)
        {
            Task t = Navigation.PushModalAsync(new AboutPage(), true);
        }

        private void OnMore(object sender, EventArgs e)
        {
            m_Account.SeeMore();
        }

        private void OnAddAccount(object sender, EventArgs e)
        {
            Task t = Navigation.PushModalAsync(
                new AccountSelectionPage(new AccountReference[]
                {
                    new AccountReference(){Nickname="Boo"},
                    new AccountReference(){Nickname="Hoo"},
                    new AccountReference(){Nickname="Quu"}
                }, null));
            //TODO: bank selection
            //await Navigation.PushModalAsync(new BankScraperPage((App.Current as App).GetBankConnection(Banks.CapitalOne)));
        }
    }
}
