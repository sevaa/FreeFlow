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
    public partial class XactCell : ViewCell
    {
        public XactCell()
        {
            InitializeComponent();
        }

        //Deposit, Withdrawal, Projected deposit, Projected withdrawal
        static private readonly Color[] s_BarColors = new Color[]
        {
            new Color(20.0/255,160.0/255,20.0/255),
            new Color(0, 0, 0),
            new Color(166.0/255, 222.0/255,166.0/255),
            new Color(100.0/255, 100.0/255, 100.0/255)
        };

        static private readonly Color[] s_BalanceColors = new Color[]
        {
            new Color(0, 0, 0),
            new Color(0.8, 0, 0)
        };

        protected override void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();

            if (BindingContext != null)
            {
                Xact x = BindingContext as Xact;
                lWhen.Text = x.When.ToString("MMM dd");
                lDesc.Text = x.Nickname ?? x.Desc;
                lAmount.Text = x.Amount.FormatAmount();
                lBalance.Text = x.Balance.FormatBalance();
                bLeft.Color = s_BarColors[(x.IsProjected ? 2 : 0) + (x.Amount < 0 ? 1 : 0)];
                lRepeat.IsVisible = x.IsRecurring;
                lWhen.FontAttributes = 
                    lAmount.FontAttributes = 
                    lBalance.FontAttributes =
                    lDesc.FontAttributes = x.IsProjected ? FontAttributes.Italic : FontAttributes.Bold;
                lWhen.TextColor =
                    lDesc.TextColor =
                    lAmount.TextColor = s_BarColors[x.Amount < 0 ? 1 : 0];
                lBalance.TextColor = s_BalanceColors[x.Balance >= 0 ? 0 : 1];
            }
        }
    }
}