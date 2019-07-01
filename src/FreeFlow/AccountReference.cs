using System;
using System.Runtime.Serialization;

namespace FreeFlow
{
    [DataContract] public class AccountReference
    {
        public AccountReference() { }

        [DataMember] public Banks Bank { get; set; }
        //Acct number or the last few digits - unique within a bank
        [DataMember] public string Code { get; set; }
        [DataMember] public string Nickname { get; set; }

        public object ExtraData;
    }
}