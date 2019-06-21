using System;
using System.Collections.Generic;
using System.Text;

namespace FreeFlow
{
    public class Xact
    {
        // Deposits are positive, withdrawals are negative
        public decimal Amount { get; set; }
        public string Desc { get; set; }
        public DateTime When { get; set; }
        // Position in the download
        public int Ordinal;
        public decimal Balance { get; set; }

        public bool IsPending { get; set; }
        public bool IsProjected { get; set; }
        public bool IsRecurring { get; set; }

        public int RecurrenceID;

        //Date - new above old, deposite above withdrawals, then retain the source order
        public static int Compare(Xact l, Xact r)
        {
            if (l.When > r.When || (l.When == r.When && ((l.Amount > 0 && r.Amount < 0) || l.Ordinal < r.Ordinal)))
                return -1;
            else
                return 1;
        }
    }
}
