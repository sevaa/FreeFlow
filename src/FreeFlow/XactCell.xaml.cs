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
        public static readonly BindableProperty
            IsRecurringProperty = BindableProperty.Create("IsRecurring", typeof(bool), typeof(XactCell), false),
            IsProjectedProperty = BindableProperty.Create("IsProjected", typeof(bool), typeof(XactCell), false),
            DescProperty = BindableProperty.Create("Desc", typeof(string), typeof(XactCell), string.Empty),
            AmountProperty = BindableProperty.Create("Amount", typeof(decimal), typeof(XactCell), (decimal)0),
            BalanceProperty = BindableProperty.Create("Balance", typeof(decimal), typeof(XactCell), (decimal)0),
            WhenProperty = BindableProperty.Create("When", typeof(DateTime), typeof(XactCell), default(DateTime));

        public XactCell()
        {
            InitializeComponent();
        }

        public string Desc
        {
            get { return (string)GetValue(DescProperty); }
            set { SetValue(DescProperty, value); }
        }

        public decimal Amount
        {
            get { return (decimal)GetValue(AmountProperty); }
            set { SetValue(AmountProperty, value); }
        }

        public decimal Balance
        {
            get { return (decimal)GetValue(BalanceProperty); }
            set { SetValue(BalanceProperty, value); }
        }

        public DateTime When
        {
            get { return (DateTime)GetValue(WhenProperty); }
            set { SetValue(WhenProperty, value); }
        }

        public bool IsRecurring
        {
            get { return (bool)GetValue(IsRecurringProperty); }
            set { SetValue(IsRecurringProperty, value); }
        }

        public bool IsProjected
        {
            get { return (bool)GetValue(IsProjectedProperty); }
            set { SetValue(IsProjectedProperty, value); }
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
                lWhen.Text = When.ToString("MMM dd");
                lDesc.Text = Desc;
                lAmount.Text = Amount.FormatAmount();
                lBalance.Text = Balance.FormatBalance();
                bLeft.Color = s_BarColors[(IsProjected ? 2 : 0) + (Amount < 0 ? 1 : 0)];
                lRepeat.IsVisible = IsRecurring;
                lWhen.FontAttributes = 
                    lAmount.FontAttributes = 
                    lBalance.FontAttributes =
                    lDesc.FontAttributes = IsProjected ? FontAttributes.Italic : FontAttributes.Bold;
                lWhen.TextColor =
                    lDesc.TextColor =
                    lAmount.TextColor = s_BarColors[Amount < 0 ? 1 : 0];
                lBalance.TextColor = s_BalanceColors[Balance >= 0 ? 0 : 1];
            }
        }
    }
}