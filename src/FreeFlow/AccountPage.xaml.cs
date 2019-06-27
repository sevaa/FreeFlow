﻿using System;
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

        public AccountPage(AccountReference Ref)
        {
            InitializeComponent();

            m_Account = new Account(Ref);
            m_Account.RecurrencesChange += OnRecurrencesChange;
            TheList.ItemsSource = m_Account.Xacts();
        }

        private void OnRecurrencesChange()
        {
            TheList.ItemsSource = m_Account.Xacts();
        }

        private void OnXactSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if(e.SelectedItem != null)
                Navigation.PushAsync(new XactDetailsPage(m_Account, e.SelectedItem as Xact), true);
        }

        private void OnRefresh(object sender, EventArgs e)
        {
            AccountReference Ref = m_Account.Ref;
            Navigation.PushModalAsync(new BankScraperPage((App.Current as App).GetBankConnection(Ref.Bank), ScraperMode.GetStatement, Ref));
        }
    }
}
