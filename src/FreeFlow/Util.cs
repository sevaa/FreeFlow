using System;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

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
            return string.Format("{1}${0}", d < 0 ? -d : d, d > 0 ? '+' : '-');
        }

        //$000.00 or -$000.00
        public static string FormatBalance(this decimal d)
        {
            return string.Format("{1}${0}", d < 0 ? -d : d, d < 0 ? "-" : string.Empty);
        }
    }

    static class ArrayUtil
    {
        public static T[] ArrayConcat<T>(this T[] a, T[] b)
        {
            T[] Result = new T[a.Length + b.Length];
            a.CopyTo(Result, 0);
            b.CopyTo(Result, a.Length);
            return Result;
        }

        public static T[] ArrayAppend<T>(this T[] a, T t)
        {
            T[] Result = new T[a.Length + 1];
            a.CopyTo(Result, 0);
            Result[a.Length] = t;
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

        public static string ToMDYString(this DateTime dt)
        {
            return dt.ToString("MM/dd/yyyy");
        }
    }

    static class JSON
    {
        static public readonly UTF8Encoding s_Encoding = new UTF8Encoding(false);

        internal static void Save<T>(string FileName, T Data)
        {
            string s;
            using (MemoryStream ms = new MemoryStream())
            {
                new DataContractJsonSerializer(typeof(T)).WriteObject(ms, Data);
                s = s_Encoding.GetString(ms.GetBuffer(), 0, (int)ms.Length);
            }
            File.WriteAllText(FileName, s);
        }

        public static T Parse<T>(string s) where T:class
        {
            if (s.StartsWith(")]}',\n"))
                s = s.Substring(6);
            using (MemoryStream ms = new MemoryStream(s_Encoding.GetBytes(s)))
                return new DataContractJsonSerializer(typeof(T)).ReadObject(ms) as T;
        }
    }
}