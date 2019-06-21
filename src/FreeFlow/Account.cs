using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;

namespace FreeFlow
{
    public class Account
    {
        private int m_IDGen;
        private List<Recurrence> m_Recurrences;
        private Xact[] m_Statement;
        private string m_RecFile;

        public event Action RecurrencesChange;

        public Account()
        {
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
            return ApplyRecurrences(m_Statement, TimeSpan.FromDays(31));
        }

        internal void DeleteRecurrence(int RecurrenceID)
        {
            Recurrence Rec;
            if ((Rec = m_Recurrences.Find(r => r.ID == RecurrenceID)) == null)
                return;

            m_Recurrences.Remove(Rec);
            SaveRecurrences();
            if (RecurrencesChange != null)
                RecurrencesChange();
        }

        private void SaveRecurrences()
        {
            string s;
            using (MemoryStream ms = new MemoryStream())
            {
                new DataContractJsonSerializer(typeof(List<Recurrence>)).WriteObject(ms, m_Recurrences);
                s = new UTF8Encoding(false).GetString(ms.GetBuffer(), 0, (int)ms.Length);
            }
            File.WriteAllText(m_RecFile, s);
        }

        #region Implementation

        //TODO: mark xacts in the statement as instances of the recurrence
        //Also work before the recurrence setup - not sure
        private Xact[] ApplyRecurrences(Xact[] Xacts, TimeSpan LookForward)
        {
            if (Xacts.Length > 0)
            {
                int OrdGen = 0;
                DateTime EndDate = Xacts[0].When, StartDate = Xacts[Xacts.Length - 1].When;
                DateTime EndOfFuture = EndDate + LookForward;
                List<Xact> Projections = new List<Xact>();

                foreach (Recurrence Rec in m_Recurrences)
                {
                    int Tolerance = Rec.Type == RecurrenceType.Weekly && Rec.Interval == 1 ? 1 : 2;
                    DateTime EndDateWithTolerance = EndDate + TimeSpan.FromDays(Tolerance);
                    for (DateTime dt = Rec.StartDate; dt < EndOfFuture; dt = Next(Rec, dt))
                    {
                        if (dt < EndOfFuture && (dt > EndDateWithTolerance || !CloseXactExists(Rec, dt, Xacts)))
                        {
                            Projections.Add(new Xact()
                            {
                                Amount = Rec.Amount,
                                Desc = Rec.XactDesc,
                                When = dt,
                                IsProjected = true,
                                IsRecurring = true,
                                RecurrenceID = Rec.ID,
                                Ordinal = OrdGen++
                            });
                        }
                    }

                }

                Xact[] aProjections = Projections.ToArray();
                Array.Sort(aProjections, Xact.Compare);
                RunBalanceForward(Xacts[0].Balance, aProjections);
                return aProjections.ArrayConcat<Xact>(Xacts);
            }
            else
                return Xacts;
        }

        internal void UpdateMonthlyRecurrence(int RecurrenceID, string Desc, decimal Amount, int MonthCount, DateTime StartDate)
        {
            UpdateRecurrence(RecurrenceID, Desc, Amount, Rec =>
            {
                Rec.Interval = MonthCount;
                Rec.StartDate = StartDate;
            }); 
        }

        internal void UpdateWeeklyRecurrence(int RecurrenceID, string Desc, decimal Amount, int WeekCount, DateTime StartDate)
        {
            UpdateRecurrence(RecurrenceID, Desc, Amount, Rec =>
            {
                Rec.Interval = WeekCount;
                Rec.StartDate = StartDate;
            });
        }

         private void UpdateRecurrence(int RecurrenceID, string Desc, decimal Amount, Action<Recurrence> SaveSchedule)
         {
            Recurrence Rec;
            if (RecurrenceID == 0)
            {
                Rec = new Recurrence()
                {
                    ID = ++m_IDGen,
                    Type = RecurrenceType.Weekly,
                    XactDesc = Desc,
                    Amount = Amount
                };
                SaveSchedule(Rec);
                m_Recurrences.Add(Rec);
            }
            else
            {
                
                if ((Rec = m_Recurrences.Find(r => r.ID == RecurrenceID)) == null)
                    return;

                Rec.XactDesc = Desc;
                Rec.Amount = Amount;
                SaveSchedule(Rec);
            }

            SaveRecurrences();
            if (RecurrencesChange != null)
                RecurrencesChange();
        }

        private bool CloseXactExists(Recurrence Rec, DateTime dt, Xact[] Xacts)
        {
            return false;
        }

        private DateTime Next(Recurrence Rec, DateTime dt)
        {
            if (Rec.Type == RecurrenceType.Weekly)
                return dt.AddDays(7 * Rec.Interval);
            else if (Rec.Type == RecurrenceType.Monthly)
                return dt.AddMonths(Rec.Interval);
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
