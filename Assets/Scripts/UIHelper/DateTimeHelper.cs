using Cysharp.Threading.Tasks;
using System;
using System.Globalization;
using System.Text;
using System.Threading;

public static class DateTimeHelper
{
    private static CultureInfo _usCulture = new CultureInfo("en-US");
    public static DateTime ToDefaultDateTime(this string dateTimeString)
    {
        if (string.IsNullOrEmpty(dateTimeString))
            return MinDateTime;

        if (DateTime.TryParseExact(
                dateTimeString,
                "yyyy-MM-dd HH:mm:ss",
                _usCulture,
                DateTimeStyles.None,
                out var result))
        {
            return result;
        }
        if (DateTime.TryParse(dateTimeString, out result))
        {
            return result;
        }

        return MinDateTime;
    }

    public static DateTime MinDateTime = new DateTime(2000, 1, 1);
    public static string ToDefaultString(this DateTime dateTime)
    {
        if (dateTime < MinDateTime) dateTime = MinDateTime;
        try
        {
            return dateTime.ToString("yyyy-MM-dd HH:mm:ss", _usCulture);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
        return dateTime.ToString("yyyy-MM-dd HH:mm:ss");
    }

    public enum TimeDisplayFormat
    {
        Standard,
        Compact,
        LessStandard,
        DayHourMinute
    }
    public static async UniTask CountDownTask(DateTime nextTime, Action<string> countDown, Action action,
                                              CancellationToken cancellationToken = default,
                                              TimeDisplayFormat displayFormat = TimeDisplayFormat.Compact)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var currentTime = DateTime.UtcNow;
            if (currentTime >= nextTime)
            {
                countDown?.Invoke("00:00");
                action?.Invoke();
                break;
            }

            var timeSpan = nextTime - currentTime;
            countDown?.Invoke(FormatTime(timeSpan, displayFormat));
            try
            {
                await UniTask.Yield(PlayerLoopTiming.Update);
            }
            catch (OperationCanceledException)
            {
                // Handle cancellation logic if necessary
                break;
            }
        }
    }

    public static string FormatTime(TimeSpan timeSpan, int numberElements = 3, bool timeChar = true)
    {
        var sb = new StringBuilder();
        int totalDays = (int)timeSpan.TotalDays;
        int hours = timeSpan.Hours;
        int minutes = timeSpan.Minutes;
        int seconds = timeSpan.Seconds;

        void AppendPart(string value)
        {
            if (sb.Length > 0)
                sb.Append(":");
            sb.Append(value);
        }
        void Append(string value) => sb.Append(value);

        var elementCount = numberElements;

        if (totalDays > 0 && elementCount > 0)
        {
            AppendPart($"{totalDays}");
            if (timeChar) Append("d");
            elementCount -= 1;
        }

        if (hours > 0 && elementCount > 0)
        {
            AppendPart($"{hours}");
            if (timeChar) Append("h");
            elementCount -= 1;
        }
        if (minutes > 0 && elementCount > 0)
        {
            AppendPart($"{minutes}");
            if (timeChar) Append("m");
            elementCount -= 1;
        }
        if (seconds > 0 && elementCount > 0)
        {
            AppendPart($"{seconds:00}");
            if (timeChar) Append("s");
            elementCount -= 1;
        }
        return sb.ToString();
    }

    public static string FormatTime(TimeSpan timeSpan, TimeDisplayFormat format)
    {
        int totalDays = (int)timeSpan.TotalDays;
        int hours = timeSpan.Hours;
        int minutes = timeSpan.Minutes;
        int seconds = timeSpan.Seconds;

        var sb = new StringBuilder();
        var isStandard = format == TimeDisplayFormat.Standard;
        var isLessStandard = format == TimeDisplayFormat.LessStandard;
        var isCompact = format == TimeDisplayFormat.Compact;
        var isDayHourMinute = format == TimeDisplayFormat.DayHourMinute;

        void AppendPart(string value)
        {
            if (sb.Length > 0)
                sb.Append(":");
            sb.Append(value);
        }

        if (isLessStandard)
        {
            if (totalDays > 0)
            {
                AppendPart($"{totalDays}d");
            }
            if (hours > 0)
            {
                AppendPart($"{hours}h");
            }
            if (minutes > 0 && totalDays == 0)
            {
                AppendPart($"{minutes}m");
            }
            if (seconds > 0 && hours == 0 && totalDays == 0)
            {
                AppendPart($"{seconds}s");
            }
            return sb.ToString();
        }

        if (totalDays > 0 || isDayHourMinute)
        {
            AppendPart((isStandard || isDayHourMinute) ? $"{totalDays}d" : $"{totalDays}");
        }

        if (hours > 0 || totalDays > 0 || isDayHourMinute)
        {
            AppendPart((isStandard || isDayHourMinute) ? $"{hours:D2}h" : $"{hours:D2}");
        }

        if (minutes > 0 || hours > 0 || totalDays > 0 || isCompact || isDayHourMinute)
        {
            AppendPart((isStandard || isDayHourMinute) ? $"{minutes:D2}m" : $"{minutes:D2}");
        }

        if (isDayHourMinute)
        {
            return sb.ToString();
        }

        // Chỉ thêm giây nếu còn lại
        if (seconds > 0 || minutes > 0 || hours > 0 || totalDays > 0)
        {
            AppendPart(isStandard ? $"{seconds:D2}s" : $"{seconds:D2}");
        }

        // Trường hợp chỉ có giây
        if (seconds >= 0 && minutes == 0 && hours == 0 && totalDays == 0)
        {
            return isStandard ? $"{seconds:D2}s" : $"00:{seconds:D2}";
        }

        return sb.ToString();
    }
}
