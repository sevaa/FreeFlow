using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;

namespace FreeFlow
{
    internal class Account
    {
        private AccountReference m_Ref;
        private int m_IDGen;
        private List<Recurrence> m_Recurrences;
        private Xact[] m_Statement;
        private string m_RecFile;

        public event Action RecurrencesChange;

        public Account(AccountReference Ref)
        {
            m_Ref = Ref;
            //Used for file naming - filename safe characters only
            string AccountCode = string.Format("{0}-{1}",
                Enum.GetName(typeof(Banks), Ref.Bank),
                Ref.Code);

            //Load the recurrences
            string LocalData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            m_RecFile = Path.Combine(LocalData, "rec.json");
            if (File.Exists(m_RecFile))
            {
                string s = File.ReadAllText(m_RecFile);
                using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(s)))
                    m_Recurrences = new DataContractJsonSerializer(typeof(List<Recurrence>)).ReadObject(ms) as List<Recurrence>;
                m_IDGen = m_Recurrences.Max(r => r.ID);
            }
            else
            {
                m_Recurrences = new List<Recurrence>();
                m_IDGen = 0;
            }

            //Load the cached statement
            m_Statement = GetCachedXacts();
        }

        public Xact[] Xacts()
        {
            ClearRecurrenceMatches();
            return ApplyRecurrences(m_Statement, TimeSpan.FromDays(31));
        }

        #region Recurrence editing

        internal Recurrence FindRecurrence(int RecurrenceID)
        {
            return m_Recurrences.Find(r => r.ID == RecurrenceID);
        }

        internal void DeleteRecurrence(int RecurrenceID)
        {
            Recurrence Rec;
            if ((Rec = FindRecurrence(RecurrenceID)) == null)
                return;

            m_Recurrences.Remove(Rec);
            SaveRecurrences();
            if (RecurrencesChange != null)
                RecurrencesChange();
        }

        private void SaveRecurrences()
        {
            JSON.Save(m_RecFile, m_Recurrences);
        }

        internal void UpdateRecurrence(Recurrence Rec, int PrevRecurrenceID)
        {
            if (Rec.ID == 0) //Newly created one
            {
                if(PrevRecurrenceID != 0) //Replacing a deleted one - happens if they click Delete and then Create
                {
                    Recurrence PrevRec;
                    if ((PrevRec = FindRecurrence(PrevRecurrenceID)) != null)
                        m_Recurrences.Remove(PrevRec);
                }
                Rec.ID = ++m_IDGen;
                m_Recurrences.Add(Rec);
            }
            //else the fields in the existing recurrence were already updated
            //TODO: check for dirtiness, recalculate only if changed?

            SaveRecurrences();
            if (RecurrencesChange != null)
                RecurrencesChange();
        }

        #endregion

        #region Recurrence implementation

        private void ClearRecurrenceMatches()
        {
            foreach (Xact x in m_Statement)
            {
                x.IsRecurring = false;
                x.RecurrenceID = 0;
                x.Nickname = null;
            }
        }

        //TODO: mark xacts in the statement as instances of the recurrence
        //Also work before the recurrence setup - not sure
        private Xact[] ApplyRecurrences(Xact[] Xacts, TimeSpan LookForward)
        {
            if (Xacts.Length > 0)
            {
                int OrdGen = 0;
                List<Xact> Projections = new List<Xact>();

                foreach (Recurrence Rec in m_Recurrences)
                    ApplyRecurrence(Rec, Projections, ref OrdGen, LookForward);

                Xact[] aProjections = Projections.ToArray();
                Array.Sort(aProjections, Xact.Compare);
                RunBalanceForward(Xacts[0].Balance, aProjections);
                return aProjections.ArrayConcat<Xact>(Xacts);
            }
            else
                return Xacts;
        }

        private void ApplyRecurrence(Recurrence Rec, List<Xact> Projections, ref int OrdGen, TimeSpan LookForward)
        {
            DateTime EndDate = m_Statement[0].When;
            DateTime EndOfFuture = EndDate + LookForward;

            TimeSpan Tolerance = TimeSpan.FromDays(Rec.Type == RecurrenceType.Weekly && Rec.Interval == 1 ? 1 : 2);
            DateTime EndDateWithTolerance = EndDate + Tolerance;
            int i = 0;
            DateTime dt;
            Xact MatchInStatement;
            for (dt = Rec.StartDate; dt < EndOfFuture; dt = GetScheduledDate(Rec, i++))
            {
                if ((MatchInStatement = FindMatchInStatement(Rec, dt, Tolerance)) != null)
                {
                    MatchInStatement.IsRecurring = true;
                    MatchInStatement.RecurrenceID = Rec.ID;
                    MatchInStatement.Nickname = Rec.Nickname;
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
                        Ordinal = OrdGen++,
                        Nickname = Rec.Nickname
                    });
                }
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

        private void RunBalanceForward(Decimal EndBalance, Xact[] a)
        {
            for(int i=a.Length-1;i>=0;--i)
                EndBalance = a[i].Balance = EndBalance + a[i].Amount;
        }

        #endregion

        //Capital One specific!!!
        #region CapitalOne

        public virtual Xact[] GetCachedXacts()
        {
            string LocalData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string s = File.ReadAllText(Path.Combine(LocalData, "balance.txt"));
            Decimal EndBalance;
            if (!Decimal.TryParse(s, out EndBalance))
                return null;
            s = File.ReadAllText(Path.Combine(LocalData, "stmt.txt"));
            string[] Lines = s.Split('\n').Skip(1).Where(Line => !string.IsNullOrEmpty(Line.Trim())).ToArray();
            Xact[] Xacts = Lines.Select(ParseXact).ToArray();
            Array.Sort(Xacts, Xact.Compare);
            RunBalanceBack(EndBalance, Xacts);
            return Xacts;
        }

        private Xact ParseXact(string s, int i)
        {
            string[] a = s.Split(',');
            return new Xact
            {
                Ordinal = i,
                Amount = Decimal.Parse(a[1].Length > 0 ? a[1] : a[2]),
                Desc = a[3].Trim(),
                When = DateTime.ParseExact(a[4], "MM/dd/yy", CultureInfo.InvariantCulture),
                IsPending = a[3].Trim().StartsWith("Memo"),
                IsProjected = false
            };
        }


        #endregion
    }
}
