using System;

namespace ASCOM.EFucoser
{
    internal static class Program
    {
        private static int Main()
        {
            try
            {
                int position;
                bool moving;
                Assert(FocuserProtocol.TryParseMotionStatus("P 12345;M true#", out position, out moving), "motion status parses");
                Assert(position == 12345 && moving, "motion status values are preserved");
                Assert(FocuserProtocol.TryParseMotionStatus(" P 42 ; M FALSE # ", out position, out moving), "motion status accepts protocol whitespace");
                Assert(position == 42 && !moving, "motion status boolean is case insensitive");
                Assert(!FocuserProtocol.TryParseMotionStatus("P x;M false#", out position, out moving), "malformed motion status is rejected");

                FocuserDeviceInfo info = FocuserProtocol.ParseDeviceInfo("{\"maxSteps\":816000,\"lastTemp\":-3.25,\"tempSensorPresent\":true}#");
                Assert(info.MaxSteps == 816000, "maximum step count parses");
                Assert(info.Temperature == -3.25, "temperature parses with invariant culture");
                Assert(info.TemperatureSensorPresent == true, "temperature sensor presence parses");

                Assert(FocuserProtocol.IsErrorResponse("ERR:home_unavailable#"), "firmware error is recognized");
                Assert(!FocuserProtocol.IsErrorResponse("OK#"), "successful response is accepted");
                Console.WriteLine("All protocol tests passed.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException("Test failed: " + message);
        }
    }
}
