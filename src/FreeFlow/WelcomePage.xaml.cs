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

        private async void OnBank(object sender, EventArgs e)
        {
            Banks Bank = (Banks)int.Parse((sender as Button).CommandParameter as string);
            BankScraperPage TheScraper = new BankScraperPage((App.Current as App).GetBankConnection(Bank));
            await Navigation.PushModalAsync(TheScraper);
        }
    }
}