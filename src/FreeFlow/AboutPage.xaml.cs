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
    public partial class AboutPage : ContentPage
    {
        public AboutPage()
        {
            InitializeComponent();
            lVersion.Text = string.Format("Version {0}", DependencyService.Get<IPlatformHelper>().Version);
        }

        private void OnSupport(object sender, EventArgs e)
        {
            Device.OpenUri(new Uri("https://docs.google.com/forms/d/e/1FAIpQLSdmGr4brXvytVp9uiVojnfAwYEjfwxuM2Ob-rTnqYhMGwwOyg/viewform"));
        }

        private void OnFB(object sender, EventArgs e)
        {
            Device.OpenUri(new Uri(""));
        }

        private void OnClose(object sender, EventArgs e)
        {
            Navigation.PopModalAsync();
        }
    }
}