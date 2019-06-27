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
    public partial class AccountSelectionPage : ContentPage
    {
        private Action<AccountReference> m_SaveSelection;
        private AccountReference [] m_Accts;

        public AccountSelectionPage(IEnumerable<AccountReference> Accts, Action<AccountReference> SaveSelection)
        {
            m_Accts = Accts.ToArray();
            m_SaveSelection = SaveSelection;
            InitializeComponent();
            TheList.ItemsSource = m_Accts;
        }

        public IEnumerable<AccountReference> Accounts => m_Accts;

        private void OnCancel(object sender, EventArgs e)
        {
            Navigation.PopModalAsync();
            m_SaveSelection(null);
        }

        private void OnSel(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItemIndex >= 0)
            {
                Navigation.PopModalAsync();
                m_SaveSelection(e.SelectedItem as AccountReference);
            }
        }
    }
}