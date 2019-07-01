using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Xamarin.Forms;

namespace FreeFlow
{
    public abstract class BankConnection
    {
        public abstract BankScraperDriver GetScraperDriver(BankScraperPage ScraperPage, AccountReference Ref);
    }
}
