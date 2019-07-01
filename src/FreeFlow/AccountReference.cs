using System;
using System.Runtime.Serialization;

namespace FreeFlow
{
    [DataContract] public class AccountReference
    {
        public AccountReference() { }

        [DataMember] public Banks Bank { get; set; }
        //Bank-side acct number or the last few digits - unique within a bank
        [DataMember] public string AccountNumber { get; set; }
        //Initially set by the system, can be renamed
        [DataMember] public string Nickname { get; set; }

        public object ExtraData;
    }
}