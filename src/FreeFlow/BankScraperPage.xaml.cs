using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration;
using Xamarin.Forms.Xaml;

namespace FreeFlow
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class BankScraperPage : ContentPage
    {
        internal BankScraperPage(BankConnection BankConn, Account Acct = null)
        {
            InitializeComponent();
            TheWeb.Source = BankConn.GetScraperDriver(this, Acct).InitialURL;
        }

        //Setting the source to a relative URL uses a bogus base
        public void BrowseTo(string URL)
        {
            TheWeb.Source = new Uri(new Uri((TheWeb.Source as UrlWebViewSource).Url), URL).ToString();
        }

        public void AddNavigatedHandler(EventHandler<WebNavigatedEventArgs> OnNavigated)
        {
            TheWeb.Navigated += OnNavigated;
        }

        public void AddNavigatingHandler(EventHandler<WebNavigatingEventArgs> OnNavigating)
        {
            TheWeb.Navigating += OnNavigating;
        }

        public Task<string> RunJS(string s)
        {
            return TheWeb.EvaluateJavaScriptAsync(s);
        }

        public void DisplayMessage(string s)
        {
            lMessage.Text = s;
        }

        public void MessageOff()
        {
            lMessage.Text = string.Empty;
        }

        private async void OnDebug(object sender, EventArgs e)
        {
            string s = "window._FF_";
            string r = await RunJS(s);
            System.Diagnostics.Debug.WriteLine(r);
        }

        private void OnCancel(object sender, EventArgs e)
        {
            Task t = Navigation.PopModalAsync();
        }
    }
}