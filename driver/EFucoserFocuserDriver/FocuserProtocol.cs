using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ASCOM.EFucoser
{
    internal sealed class FocuserDeviceInfo
    {
        public int? MaxSteps { get; set; }
        public double? Temperature { get; set; }
        public bool? TemperatureSensorPresent { get; set; }
    }

    internal static class FocuserProtocol
    {
        private static readonly Regex MotionStatusPattern = new Regex(
            @"^\s*P\s+(-?\d+)\s*;\s*M\s+(true|false)\s*#?\s*$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        internal static bool TryParseMotionStatus(string response, out int position, out bool moving)
        {
            position = 0;
            moving = false;

            Match match = MotionStatusPattern.Match(response ?? string.Empty);
            return match.Success
                && int.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out position)
                && bool.TryParse(match.Groups[2].Value, out moving);
        }

        internal static FocuserDeviceInfo ParseDeviceInfo(string response)
        {
            return new FocuserDeviceInfo
            {
                MaxSteps = ParseNullableInt(response, "maxSteps"),
                Temperature = ParseNullableDouble(response, "lastTemp"),
                TemperatureSensorPresent = ParseNullableBool(response, "tempSensorPresent")
            };
        }

        internal static bool IsErrorResponse(string response)
        {
            return !string.IsNullOrWhiteSpace(response)
                && response.TrimStart().StartsWith("ERR:", StringComparison.OrdinalIgnoreCase);
        }

        private static int? ParseNullableInt(string response, string propertyName)
        {
            Match match = MatchJsonValue(response, propertyName, @"-?\d+");
            int value;
            if (match.Success && int.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                return value;
            return null;
        }

        private static double? ParseNullableDouble(string response, string propertyName)
        {
            Match match = MatchJsonValue(response, propertyName, @"-?(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][+-]?\d+)?");
            double value;
            if (match.Success && double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                return value;
            return null;
        }

        private static bool? ParseNullableBool(string response, string propertyName)
        {
            Match match = MatchJsonValue(response, propertyName, @"true|false");
            bool value;
            if (match.Success && bool.TryParse(match.Groups[1].Value, out value))
                return value;
            return null;
        }

        private static Match MatchJsonValue(string response, string propertyName, string valuePattern)
        {
            return Regex.Match(
                response ?? string.Empty,
                "\\\"" + Regex.Escape(propertyName) + "\\\"\\s*:\\s*(" + valuePattern + ")",
                RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        }
    }
}
