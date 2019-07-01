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
    public partial class WelcomePage : ContentPage
    {
        public WelcomePage()
        {
            InitializeComponent();
        }

        private void OnBank(object sender, EventArgs e)
        {
            App TheApp = App.Current as App;
            Banks Bank = (Banks)int.Parse((sender as Button).CommandParameter as string);
            BankScraperPage TheScraper = new BankScraperPage(TheApp.GetBankConnection(Bank));
            Task t = Navigation.PushModalAsync(TheScraper);
            TheApp.AccountRegistered += OnConnected;
        }

        private void OnConnected(Account NewAcct)
        {
            Task t = Navigation.PushAsync(new AccountPage(NewAcct));
            Navigation.RemovePage(this);
        }
    }
}