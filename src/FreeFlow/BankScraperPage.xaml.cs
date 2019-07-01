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
        public BankScraperPage(BankConnection BankConn, AccountReference Ref = null)
        {
            InitializeComponent();
            TheWeb.Source = BankConn.GetScraperDriver(this, Ref).InitialURL();
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
            lMessage.IsVisible = true;
        }

        public void MessageOff()
        {
            lMessage.IsVisible = false;
        }

        private async void OnDebug(object sender, EventArgs e)
        {
            string s = "window._FF_";
            string r = await RunJS(s);
            System.Diagnostics.Debug.WriteLine(r);
        }
    }
}