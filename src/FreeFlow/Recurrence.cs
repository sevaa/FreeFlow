using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace FreeFlow
{
    [DataContract]
    internal class Recurrence
    {
        public Recurrence() { }

        [DataMember]public int ID { get; set; }
        [DataMember] public string XactDesc { get; set; }
        [DataMember] public decimal Amount { get; set; }
        [DataMember] public string Nickname { get; set; }
        [DataMember] public RecurrenceType Type { get; set; }
        //Type specific recurrence parameters - uniform for now, may change later
        [DataMember] public DateTime StartDate { get; set; }
        [DataMember] public int Interval { get; set; } //Week/month count
    }
}
