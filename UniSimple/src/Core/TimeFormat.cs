using System.Text;
using System.Threading;

namespace UniSimple
{
    public static class TimeFormat
    {
        public static string DayUnit { get; set; } = "d";

        private static readonly ThreadLocal<StringBuilder> ThreadStringBuilder = new(() => new StringBuilder(64));
        private static StringBuilder Builder => ThreadStringBuilder.Value;

        /// <summary>
        /// 格式化秒数为时间字符串
        /// 格式1: 大于等于1天 "1d 05:03:20"
        /// 格式2: 小于1天 "05:03:20"
        /// </summary>
        public static string FormatSeconds(this long totalSeconds)
        {
            if (totalSeconds < 0) totalSeconds = 0;

            var days = totalSeconds / 86400;
            var remainder = totalSeconds % 86400;
            var hours = remainder / 3600;
            var minutes = remainder % 3600 / 60;
            var seconds = remainder % 60;

            var sb = Builder;
            sb.Clear();

            // 如果大于1天
            if (days > 0)
            {
                sb.Append(days).Append(DayUnit).Append(' ');
            }

            // HH:
            if (hours < 10) sb.Append('0');
            sb.Append(hours).Append(':');

            // MM:
            if (minutes < 10) sb.Append('0');
            sb.Append(minutes).Append(':');

            // SS
            if (seconds < 10) sb.Append('0');
            sb.Append(seconds);

            return sb.ToString();
        }

        /// <summary>
        /// 格式化秒数为时间字符串
        /// 格式1: 大于等于1天 "1d 05:03:20"
        /// 格式2: 小于1天 "05:03:20"
        /// </summary>
        public static string FormatSeconds(this int totalSeconds)
        {
            return FormatSeconds((long)totalSeconds);
        }

        public static string FormatMilliseconds(this int milliseconds)
        {
            return FormatMilliseconds((long)milliseconds);
        }

        public static string FormatMilliseconds(this long milliseconds)
        {
            return FormatSeconds(milliseconds / 1000);
        }
    }
}