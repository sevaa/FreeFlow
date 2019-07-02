using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace FreeFlow
{
    [DataContract]internal class Xact
    {
        public Xact() { }

        // Deposits are positive, withdrawals are negative
        [DataMember] public decimal Amount { get; set; }
        [DataMember] public string Desc { get; set; }
        [DataMember] public DateTime When { get; set; }
        // Position in the download - not really used
        public int Ordinal;
        [DataMember] public decimal Balance { get; set; }

        [DataMember] public bool IsPending { get; set; }
        public bool IsProjected { get; set; }
        public bool IsRecurring { get; set; }

        public int RecurrenceID;
        public string Nickname;
        public int RecurrenceDelta;

        //Date - new above old, deposits above withdrawals, then retain the source order
        public static int Compare(Xact l, Xact r)
        {
            if (l.When > r.When || (l.When == r.When && ((l.Amount > 0 && r.Amount < 0) || l.Ordinal < r.Ordinal)))
                return -1;
            else
                return 1;
        }
    }
}
