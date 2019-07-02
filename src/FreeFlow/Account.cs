using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using Xamarin.Forms;

namespace FreeFlow
{
    internal class Account
    {
        private AccountReference m_Ref;
        private int m_RecurrenceIDGenerator;
        private AccountSettings m_Settings;
        private Xact[] m_Statement;
        private string m_AccountCode, m_SettingsFile, m_SnapshotFile;
        private DateTime m_StatementDownloadDate;
        private int m_Horizon = 31; //Look-ahead horizon

        public event Action Change; //Fired for every event that changes the transaction list

        #region Data file formats
        [DataContract]private class AccountSnapshot
        {
            public AccountSnapshot() { }

            [DataMember] public Xact[] Statement { get; set; }
            [DataMember] public DateTime When { get; set; }
        }

        [DataContract] private class AccountSettings
        {
            public AccountSettings() { }

            [DataMember] public List<Recurrence> Recurrences { get; set; }
        }
        #endregion

        public AccountReference Ref => m_Ref;

        public bool NoRecurrences => m_Settings.Recurrences.Count == 0;

        public Account(AccountReference Ref)
        {
            m_Ref = Ref;
            //Used for file naming - filename safe characters only
            m_AccountCode = string.Format("{0}-{1}",
                Enum.GetName(typeof(Banks), Ref.Bank), Ref.AccountNumber);

            m_SnapshotFile = Path.Combine(App.LocalData, m_AccountCode + "-Snapshot.json");

            //Load the account settings
            m_SettingsFile = Path.Combine(App.LocalData, m_AccountCode + "-Settings.json");
            if (File.Exists(m_SettingsFile))
            {
                m_Settings = JSON.LoadParse<AccountSettings>(m_SettingsFile);
                m_RecurrenceIDGenerator = m_Settings.Recurrences.Max(r => r.ID);
            }
            else
            {
                m_Settings = new AccountSettings() { Recurrences = new List<Recurrence>() };
                m_RecurrenceIDGenerator = 0;
            }

            //Load the snapshot
            if (File.Exists(m_SnapshotFile))
            {
                AccountSnapshot Snapshot = JSON.LoadParse<AccountSnapshot>(m_SnapshotFile);
                m_Statement = Snapshot.Statement;
                m_StatementDownloadDate = Snapshot.When;
            }
            else
            {
                m_Statement = new Xact[0];
                m_StatementDownloadDate = DateTime.Today;
            }
        }

        //Interface for the account snapshot page
        public Xact[] Xacts()
        {
            ClearRecurrenceMatches();
            return ApplyRecurrences(m_Statement, TimeSpan.FromDays(m_Horizon));
        }

        public void SeeMore()
        {
            m_Horizon += 30;
            FireChange();
        }

        #region Recurrence editing

        internal Recurrence FindRecurrence(int RecurrenceID)
        {
            return m_Settings.Recurrences.Find(r => r.ID == RecurrenceID);
        }

        internal void DeleteRecurrence(int RecurrenceID)
        {
            Recurrence Rec;
            if ((Rec = FindRecurrence(RecurrenceID)) == null)
                return;

            m_Settings.Recurrences.Remove(Rec);
            SaveSettings();
            FireChange();
        }

        private void SaveSettings()
        {
            JSON.Save(m_SettingsFile, m_Settings);
        }

        internal void UpdateRecurrence(Recurrence Rec, int PrevRecurrenceID)
        {
            if (Rec.ID == 0) //Newly created one
            {
                if(PrevRecurrenceID != 0) //Replacing a deleted one - happens if they click Delete and then Create
                {
                    Recurrence PrevRec;
                    if ((PrevRec = FindRecurrence(PrevRecurrenceID)) != null)
                        m_Settings.Recurrences.Remove(PrevRec);
                }
                Rec.ID = ++m_RecurrenceIDGenerator;
                m_Settings.Recurrences.Add(Rec);
            }
            //else the fields in the existing recurrence were already updated
            //TODO: check for dirtiness, recalculate only if changed?

            SaveSettings();
            FireChange();
        }

        #endregion

        #region Recurrence implementation

        private void FireChange()
        {
            if (Change != null)
                Change();
        }

        private void ClearRecurrenceMatches()
        {
            foreach (Xact x in m_Statement)
            {
                x.IsRecurring = false;
                x.RecurrenceID = 0;
                x.Nickname = null;
            }
        }

        //Also work before the recurrence setup - not sure
        private Xact[] ApplyRecurrences(Xact[] Xacts, TimeSpan LookForward)
        {
            if (Xacts.Length > 0)
            {
                int OrdGen = 0;
                List<Xact> Projections = new List<Xact>();

                foreach (Recurrence Rec in m_Settings.Recurrences)
                    ApplyRecurrence(Rec, Projections, ref OrdGen, LookForward);

                Xact[] aProjections = Projections.ToArray();
                Array.Sort(aProjections, Xact.Compare); //By date descennding, then deposits before withdrawals
                RunBalanceForward(Xacts[0].Balance, aProjections);
                return aProjections.ArrayConcat<Xact>(Xacts);
            }
            else
                return Xacts;
        }

        //Both create projected transactions and mark existing transactions as recurrence instances
        private void ApplyRecurrence(Recurrence Rec, List<Xact> Projections, ref int OrdGen, TimeSpan LookForward)
        {
            DateTime EndDate = m_Statement[0].When;
            DateTime EndOfFuture = EndDate + LookForward;

            TimeSpan Tolerance = TimeSpan.FromDays(Rec.Type == RecurrenceType.Weekly && Rec.Interval == 1 ? 1 : 2);
            DateTime EndDateWithTolerance = EndDate + Tolerance;
            int i = 0;
            DateTime dt;
            Xact MatchInStatement;
            for (dt = Rec.StartDate; dt < EndOfFuture; dt = GetScheduledDate(Rec, i))
            {
                if ((MatchInStatement = FindMatchInStatement(Rec, dt, Tolerance)) != null)
                {
                    MatchInStatement.IsRecurring = true;
                    MatchInStatement.RecurrenceID = Rec.ID;
                    MatchInStatement.Nickname = Rec.Nickname;
                    MatchInStatement.RecurrenceDelta = i;
                }
                else if (dt >= EndDateWithTolerance)
                {
                    Projections.Add(new Xact()
                    {
                        Amount = Rec.Amount,
                        Desc = Rec.XactDesc,
                        When = dt,
                        IsProjected = true,
                        IsRecurring = true,
                        RecurrenceID = Rec.ID,
                        RecurrenceDelta = i,
                        Ordinal = OrdGen++,
                        Nickname = Rec.Nickname
                    });
                }
                ++i;
            }

            //Off chance that we have to run the schedule back
            DateTime StartDateWithTolerance = m_Statement[m_Statement.Length - 1].When - Tolerance;
            if (Rec.StartDate > StartDateWithTolerance)
            {
                for(i = -1; (dt = GetScheduledDate(Rec, i)) > StartDateWithTolerance; --i)
                    if ((MatchInStatement = FindMatchInStatement(Rec, dt, Tolerance)) != null)
                    {
                        MatchInStatement.IsRecurring = true;
                        MatchInStatement.RecurrenceID = Rec.ID;
                        MatchInStatement.Nickname = Rec.Nickname;
                        MatchInStatement.RecurrenceDelta = i;
                    }
            }
        }

        private Xact FindMatchInStatement(Recurrence Rec, DateTime dt, TimeSpan Tolerance)
        {
            TimeSpan ToleranceSpan = Tolerance;
            DateTime From = dt.Subtract(ToleranceSpan),
                To = dt.Add(ToleranceSpan);
            return m_Statement.FirstOrDefault(x => x.Desc.StartsWith(Rec.XactDesc) && x.When >= From && x.When <= To);
        }

        private DateTime GetScheduledDate(Recurrence Rec, int i)
        {
            if (Rec.Type == RecurrenceType.Weekly)
                return Rec.StartDate.AddDays(i * 7 * Rec.Interval);
            else if (Rec.Type == RecurrenceType.Monthly)
                return Rec.StartDate.AddMonths(i * Rec.Interval);
            else
                return default(DateTime);
        }

        private void RunBalanceBack(Decimal EndBalance, Xact[] a)
        {
            foreach (Xact x in a)
            {
                x.Balance = EndBalance;
                EndBalance -= x.Amount;
            }
        }

        public static void RunBalanceForward(Decimal EndBalance, Xact[] a)
        {
            for(int i=a.Length-1;i>=0;--i)
                EndBalance = a[i].Balance = EndBalance + a[i].Amount;
        }

        #endregion

        #region Interface for the scraper page
        internal void OnDownloadedTransactions(Xact[] Xacts)
        {
            m_Statement = Xacts;
            SaveSnapshot();
            FireChange();
        }

        private void SaveSnapshot()
        {
            JSON.Save(m_SnapshotFile, new AccountSnapshot()
            {
                Statement = m_Statement,
                When = DateTime.Today
            });
        }
        #endregion
    }
}
