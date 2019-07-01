using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Xamarin.Forms;

namespace FreeFlow
{
    internal abstract class BankConnection
    {
        public abstract BankScraperDriver GetScraperDriver(BankScraperPage ScraperPage, Account Acct = null);
    }
}
