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
            Banks Bank = (Banks)int.Parse((sender as Button).CommandParameter as string);
            Navigation.PushModalAsync(new BankScraperPage((App.Current as App).GetBankConnection(Bank), ScraperMode.Connect));
        }
    }
}