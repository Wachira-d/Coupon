using System;

namespace Coupon.Helpers
{
    /// <summary>
    /// Helper class for date conversions between Buddhist Era (BE) and Christian Era (CE)
    /// </summary>
    public static class DateHelper
    {
        // Buddhist Era offset (BE year = CE year + 543)
        private const int BE_OFFSET = 543;

        // Threshold to determine if a year is BE or CE
        // Any year >= 2400 is assumed to be BE (since CE years won't reach 2400 until year 2400)
        private const int BE_THRESHOLD = 2400;

        /// <summary>
        /// Check if a year is Buddhist Era (BE)
        /// </summary>
        /// <param name="year">The year to check</param>
        /// <returns>True if the year appears to be BE</returns>
        public static bool IsBuddhistEra(int year)
        {
            return year >= BE_THRESHOLD;
        }

        /// <summary>
        /// Convert Buddhist Era (BE) year to Christian Era (CE) year
        /// </summary>
        /// <param name="beYear">Buddhist Era year</param>
        /// <returns>Christian Era year</returns>
        public static int BEToCE(int beYear)
        {
            return beYear - BE_OFFSET;
        }

        /// <summary>
        /// Convert Christian Era (CE) year to Buddhist Era (BE) year
        /// </summary>
        /// <param name="ceYear">Christian Era year</param>
        /// <returns>Buddhist Era year</returns>
        public static int CEToBE(int ceYear)
        {
            return ceYear + BE_OFFSET;
        }

        /// <summary>
        /// Normalize a year to CE (Christian Era)
        /// If the year is BE (>= 2400), convert to CE; otherwise return as is
        /// </summary>
        /// <param name="year">Year to normalize</param>
        /// <returns>Year in CE format</returns>
        public static int NormalizeToCE(int year)
        {
            if (IsBuddhistEra(year))
            {
                return BEToCE(year);
            }
            return year;
        }

        /// <summary>
        /// Normalize a DateTime to use CE year
        /// If the year is BE (>= 2400), convert to CE; otherwise return as is
        /// </summary>
        /// <param name="date">Date to normalize</param>
        /// <returns>Date with CE year</returns>
        public static DateTime NormalizeDateToCE(DateTime date)
        {
            if (IsBuddhistEra(date.Year))
            {
                return new DateTime(BEToCE(date.Year), date.Month, date.Day,
                    date.Hour, date.Minute, date.Second, date.Millisecond);
            }
            return date;
        }

        /// <summary>
        /// Parse a date string and normalize to CE
        /// Handles both BE and CE year inputs
        /// </summary>
        /// <param name="dateString">Date string to parse</param>
        /// <returns>DateTime in CE format</returns>
        public static DateTime ParseAndNormalize(string dateString)
        {
            DateTime date = DateTime.Parse(dateString);
            return NormalizeDateToCE(date);
        }

        /// <summary>
        /// Try to parse a date string and normalize to CE
        /// </summary>
        /// <param name="dateString">Date string to parse</param>
        /// <param name="result">Parsed and normalized DateTime</param>
        /// <returns>True if parsing succeeded</returns>
        public static bool TryParseAndNormalize(string dateString, out DateTime result)
        {
            if (DateTime.TryParse(dateString, out DateTime date))
            {
                result = NormalizeDateToCE(date);
                return true;
            }
            result = DateTime.MinValue;
            return false;
        }
    }
}
