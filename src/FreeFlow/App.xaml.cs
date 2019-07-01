using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace FreeFlow
{
    public partial class App : Application
    {
        [DataContract] private class Config
        {
            public Config() { }
            [DataMember] public AccountReference[] Accounts { get; set; }
        }

        public static readonly string LocalData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        private Config m_Config;
        private string m_ConfigPath;
        private static readonly Dictionary<Banks, BankConnection> s_Banks = new Dictionary<Banks, BankConnection>()
        {
            {Banks.CapitalOne, new Bank.CapOne()},
            {Banks.BankOfAmerica, new Bank.BofA()}
        };

        internal event Action<Account> AccountRegistered;

        public App()
        {
            InitializeComponent();

            m_ConfigPath = Path.Combine(LocalData, "Config.json");

            if (File.Exists(m_ConfigPath))
            {
                string s = File.ReadAllText(m_ConfigPath);
                using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(s)))
                    m_Config = new DataContractJsonSerializer(typeof(Config)).ReadObject(ms) as Config;
            }
            else
                m_Config = new Config() { Accounts = new AccountReference[0] };

            MainPage = new NavigationPage(m_Config.Accounts.Length > 0 ?
                new AccountPage(m_Config.Accounts[0]) as ContentPage :
                new WelcomePage());
        }

        private void SaveConfig()
        {
            JSON.Save(m_ConfigPath, m_Config);
        }

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }

        //Assumes bank connections don't have a lot of data or expensive init
        internal BankConnection GetBankConnection(Banks Bank)
        {
            return s_Banks[Bank];
        }

        internal bool ManualLogin(Banks m_Bank)
        {
            return true; //TODO
        }

        internal Account RegisterAccount(AccountReference Ref)
        {
            m_Config.Accounts = m_Config.Accounts.ArrayAppend(Ref);
            SaveConfig();
            Account NewAccount = new Account(Ref);
            if (AccountRegistered != null)
                AccountRegistered(NewAccount);
            return NewAccount;
        }

        internal AccountReference DefaultAccount => m_Config.Accounts?[0];
    }
}
