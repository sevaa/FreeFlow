using FreeFlow.BankAdapter;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace FreeFlow
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class XactDetails : ContentPage
    { 
        private Xact m_Xact;
        private Account m_Acct;
        private RecurrenceType? m_RecType = null;

        internal XactDetails(Account Acct, Xact x)
        {
            m_Acct = Acct;
            m_Xact = x;
            InitializeComponent();

            lCreateRec.IsVisible = !x.IsRecurring;
            gRecHeader.IsVisible =
                bDeleteRec.IsVisible = x.IsRecurring;

            gMonthlyRecDetails.IsVisible = x.IsRecurring; //TODO
            gWeeklyRecDetails.IsVisible = x.IsRecurring;

            lDate.Text = x.When.ToString("ddd, MMM d");
            lDesc.Text = x.Desc;
            lAmount.Text = x.Amount.FormatAmount();
        }

        private void OnDeleteRecurrence(object sender, EventArgs e)
        {
            m_RecType = null;
            lCreateRec.IsVisible = true;
            gRecHeader.IsVisible =
                gMonthlyRecDetails.IsVisible =
                gWeeklyRecDetails.IsVisible =
                bDeleteRec.IsVisible = false;
        }

        private void OnCreateWeeklyRecurrence(object sender, EventArgs e)
        {
            lCreateRec.IsVisible = false;
            bDeleteRec.IsVisible = true;
            gWeeklyRecDetails.IsVisible = true;

            eRecDesc.Text = m_Xact.Desc;
            eRecAmount.Text = m_Xact.Amount.ToString();

            m_RecType = RecurrenceType.Weekly;
            eWeekCount.Text = "1";
            sWeekCount.Value = 1;
            eWeeklyStartDate.Text = m_Xact.When.ToString("MM/dd/yyyy");
        }

        private void OnCreateMonthlyRecurrence(object sender, EventArgs e)
        {
            lCreateRec.IsVisible = false;
            bDeleteRec.IsVisible = true;
            gMonthlyRecDetails.IsVisible = true;

            eRecDesc.Text = m_Xact.Desc;
            eRecAmount.Text = m_Xact.Amount.ToString();

            m_RecType = RecurrenceType.Monthly;
            eMonthCount.Text = "1";
            eMonthlyStartDate.Text = m_Xact.When.ToString("MM/dd/yyyy");
        }

        private void OnSave(object sender, EventArgs e)
        {
            if(m_RecType.HasValue)
            {
                DateTime dt;
                string Desc;
                decimal Amount; ;
                Desc = eRecDesc.Text.Trim();
                if (string.IsNullOrEmpty(Desc))
                    return; //TODO: feedback
                if (!decimal.TryParse(eRecAmount.Text, out Amount))
                    return;

                if (m_RecType == RecurrenceType.Weekly)
                {
                    if(!DateTimeUtil.TryParseMDY(eWeeklyStartDate.Text, out dt))
                        return; //TODO: feedback

                    m_Acct.UpdateWeeklyRecurrence(m_Xact.RecurrenceID, Desc, Amount, (int)sWeekCount.Value, dt);
                }
                else if(m_RecType == RecurrenceType.Monthly)
                {
                    if (!DateTimeUtil.TryParseMDY(eMonthlyStartDate.Text, out dt))
                        return; //TODO: feedback

                    m_Acct.UpdateMonthlyRecurrence(m_Xact.RecurrenceID, Desc, Amount, 0, default(DateTime));
                }
            }
            else if (m_Xact.RecurrenceID > 0)
                m_Acct.DeleteRecurrence(m_Xact.RecurrenceID);
            Navigation.PopModalAsync();
        }

        private void OnCancel(object sender, EventArgs e)
        {
            Navigation.PopModalAsync();
        }

        private void OnWeeklyStartDateChanged(object sender, TextChangedEventArgs e)
        {
            DateTime dt;
            if (DateTime.TryParseExact(eWeeklyStartDate.Text, "dd/MM/yyyy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowLeadingWhite | DateTimeStyles.AllowTrailingWhite,
                out dt))
            {
                lDayOfWeek.Text = dt.ToString(", dddd");
            }
        }
    }
}