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
    public partial class XactDetailsPage : ContentPage
    { 
        private Xact m_Xact;
        private Account m_Acct;
        private Recurrence m_Recurrence = null;
        private bool m_WasRecurring;

        internal XactDetailsPage(Account Acct, Xact x)
        {
            m_Acct = Acct;
            m_Xact = x;
            InitializeComponent();

            lCreateRec.IsVisible = !x.IsRecurring;
            gRecHeader.IsVisible =
                bDeleteRec.IsVisible = x.IsRecurring;

            if (x.IsRecurring)
            {
                m_Recurrence = Acct.FindRecurrence(x.RecurrenceID);

                if (m_Recurrence != null)
                {
                    m_WasRecurring = true;
                    eRecAmount.Text = m_Recurrence.Amount.ToString();
                    eRecDesc.Text = m_Recurrence.XactDesc;
                    eRecNickname.Text = m_Recurrence.Nickname;

                    if (m_Recurrence.Type == RecurrenceType.Weekly)
                    {
                        gWeeklyRecDetails.IsVisible = true;
                        eWeeklyStartDate.Text = m_Recurrence.StartDate.ToString("MM/dd/yyyy");
                        sWeekCount.Value = m_Recurrence.Interval;
                        eWeekCount.Text = m_Recurrence.Interval.ToString();
                    }
                    else if (m_Recurrence.Type == RecurrenceType.Monthly)
                    {
                        gMonthlyRecDetails.IsVisible = true;
                        eMonthlyStartDate.Text = m_Recurrence.StartDate.ToString("MM/dd/yyyy");
                        sMonthCount.Value = m_Recurrence.Interval;
                        eMonthCount.Text = m_Recurrence.Interval.ToString();
                    }
                }
            }

            lHeader.Text = string.Format("{0} {1} of ${2} on {3:ddd, MMM d}",
                x.IsProjected ? "Projected" : "Recorded",
                x.Amount < 0 ? "withdrawal" : "deposit",
                x.Amount < 0 ? -x.Amount : x.Amount,
                x.When);
            lDesc.Text = x.Desc;
        }

        private void OnDeleteRecurrence(object sender, EventArgs e)
        {
            m_Recurrence = null;
            lCreateRec.IsVisible = true;
            gRecHeader.IsVisible =
                gMonthlyRecDetails.IsVisible =
                gWeeklyRecDetails.IsVisible =
                bDeleteRec.IsVisible = false;
        }

        private void OnCreateWeeklyRecurrence(object sender, EventArgs e)
        {
            lCreateRec.IsVisible = false;
            bDeleteRec.IsVisible =
                gRecHeader.IsVisible =
                gWeeklyRecDetails.IsVisible = true;

            m_Recurrence = new Recurrence()
            {
                ID = 0,
                Type = RecurrenceType.Weekly
            };

            eRecDesc.Text = m_Xact.Desc;
            eRecAmount.Text = m_Xact.Amount.ToString();

            eWeekCount.Text = "1";
            sWeekCount.Value = 1;
            eWeeklyStartDate.Text = m_Xact.When.ToString("MM/dd/yyyy");
        }

        private void OnCreateMonthlyRecurrence(object sender, EventArgs e)
        {
            lCreateRec.IsVisible = false;
            bDeleteRec.IsVisible = 
                gRecHeader.IsVisible =
                gMonthlyRecDetails.IsVisible = true;

            m_Recurrence = new Recurrence()
            {
                ID = 0,
                Type = RecurrenceType.Monthly
            };

            eRecDesc.Text = m_Xact.Desc;
            eRecAmount.Text = m_Xact.Amount.ToString();

            eMonthCount.Text = "1";
            eMonthlyStartDate.Text = m_Xact.When.ToMDYString();
        }

        private void OnWeeklyStartDateChanged(object sender, TextChangedEventArgs e)
        {
            DateTime dt;
            if (DateTimeUtil.TryParseMDY(eWeeklyStartDate.Text, out dt))
                lDayOfWeek.Text = dt.ToString(", dddd");
        }

        private void OnWeeklyIntervalStep(object sender, ValueChangedEventArgs e)
        {
            eWeekCount.Text = Convert.ToInt32(sWeekCount.Value).ToString();
        }

        private void OnMonthlyIntervalStep(object sender, ValueChangedEventArgs e)
        {
            eMonthCount.Text = Convert.ToInt32(sMonthCount.Value).ToString();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            if (m_Recurrence != null)
            {
                DateTime StartDate = default(DateTime);
                string RecDesc;
                decimal RecAmount; ;
                RecDesc = eRecDesc.Text?.Trim();
                if (string.IsNullOrEmpty(RecDesc))
                    return; //TODO: feedback
                if (!decimal.TryParse(eRecAmount.Text, out RecAmount))
                    return;
                string Nickname = eRecNickname.Text?.Trim();
                int Interval = 0;

                if (m_Recurrence.Type == RecurrenceType.Weekly)
                {
                    if (!DateTimeUtil.TryParseMDY(eWeeklyStartDate.Text, out StartDate))
                        return; //TODO: feedback
                    Interval = (int)sWeekCount.Value;
                }
                else if (m_Recurrence.Type == RecurrenceType.Monthly)
                {
                    if (!DateTimeUtil.TryParseMDY(eMonthlyStartDate.Text, out StartDate))
                        return; //TODO: feedback
                    Interval = (int)sMonthCount.Value;
                }

                //Validation passed...
                //Type can't change without a delete/recreate
                m_Recurrence.XactDesc = RecDesc;
                m_Recurrence.Amount = RecAmount;
                m_Recurrence.Nickname = Nickname;
                m_Recurrence.StartDate = StartDate;
                m_Recurrence.Interval = Interval;

                m_Acct.UpdateRecurrence(m_Recurrence, m_Xact.RecurrenceID);
            }
            else if (m_WasRecurring)
                m_Acct.DeleteRecurrence(m_Xact.RecurrenceID);
        }
    }
}