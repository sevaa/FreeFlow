using System;
using System.Globalization;

namespace FreeFlow
{
    static class StringUtil
    {
        public static string[] Split(this string s, char c)
        {
            return s.Split(new char[] { c });
        }
    }

    static class DecimalUtil
    {
        //+$000.00 or -$000.00
        public static string FormatAmount(this decimal d)
        {
            return string.Format("{1}${0}", d, d > 0 ? '+' : '-');
        }

        //$000.00 or -$000.00
        public static string FormatBalance(this decimal d)
        {
            return string.Format("{1}${0}", d, d < 0 ? "-" : string.Empty);
        }
    }

    static class ArrayUtil
    {
        public static T[] ArrayConcat<T>(this T []a, T []b)
        {
            T[] Result = new T[a.Length + b.Length];
            a.CopyTo(Result, 0);
            b.CopyTo(Result, a.Length);
            return Result;
        }
    }

    static class DateTimeUtil
    {
        public static bool TryParseMDY(string s, out DateTime dt)
        {
            return DateTime.TryParseExact(s, "MM/dd/yyyy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowLeadingWhite | DateTimeStyles.AllowTrailingWhite,
                out dt);
        }
    }
}