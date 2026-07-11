using System;
using System.Collections;
using System.Globalization;
using System.Runtime.InteropServices;
using ASCOM;
using ASCOM.Astrometry.AstroUtils;
using ASCOM.DeviceInterface;
using ASCOM.Utilities;

namespace ASCOM.EFucoser
{
    [Guid("8a3f5c72-1b6d-4e9a-a2c1-7d8e6f3a4b5c")]
    [ProgId("ASCOM.EFucoser.Focuser")]
    [ClassInterface(ClassInterfaceType.None)]
    public class Focuser : IFocuserV3
    {
        private IFocuserConnection connection;
        private System.Threading.Mutex mutex = new System.Threading.Mutex();

        private int lastPos = 0;
        private bool lastMoving = false;
        private bool lastLink = false;
        private double lastTemp = 20.0;

        private long UPDATETICKS = (long)(1 * 10000000.0);
        private long lastUpdate = 0;
        private long lastL = 0;

        // ASCOM identity
        internal static string driverID = "ASCOM.EFucoser.Focuser";
        private static string driverDescription = "ASCOM Focuser Driver for EFucoser ESP8266.";
        private const string Esp8266FocuserIdentity = "EFucoser ESP8266 Focuser";
        private const string Esp8266Uln2003FocuserIdentity = "EFucoser ESP8266 ULN2003 Focuser";
        private const string ArduinoNanoUln2003FocuserIdentity = "EFucoser Arduino Nano ULN2003 Focuser";

        // Profile keys
        internal static string comPortProfileName = "COM Port";
        internal static string comPortDefault = "COM1";
        internal static string comPortLegacyProfileName = "ComPort";
        internal static string transportProfileName = "Transport";
        internal static string transportDefault = "TCP";
        internal static string tcpHostProfileName = "TcpHost";
        internal static string tcpHostDefault = "192.168.4.1";
        internal static string tcpPortProfileName = "TcpPort";
        internal static string tcpPortDefault = "4030";
        internal static string commandTimeoutProfileName = "CommandTimeoutMs";
        internal static string commandTimeoutDefault = "3000";
        internal static string traceStateProfileName = "Trace Level";
        internal static string traceStateDefault = "false";

        // Configuration
        internal static string comPort;
        internal static string transport;
        internal static string tcpHost;
        internal static int tcpPort;
        internal static int commandTimeoutMs;
        internal static bool traceState;
        internal static int maxStep;
        internal static int stepSize = 1;

        private bool connectedState;
        private Util utilities;
        private AstroUtils astroUtilities;
        private TraceLogger tl;

        public Focuser()
        {
            ReadProfile();
            tl = new TraceLogger("", "EFucoser Focuser");
            tl.Enabled = traceState;
            tl.LogMessage("Focuser", "Starting initialization");

            connectedState = false;
            utilities = new Util();
            astroUtilities = new AstroUtils();

            tl.LogMessage("Focuser", "Completed initialization");
        }

        // ==================== IFocuserV3 Implementation ====================

        public void SetupDialog()
        {
            if (IsConnected)
                System.Windows.Forms.MessageBox.Show("Already connected, just press OK");

            using (SetupDialogForm F = new SetupDialogForm())
            {
                var result = F.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    WriteProfile();
                }
            }
        }

        public ArrayList SupportedActions
        {
            get
            {
                var sa = new ArrayList();
                sa.Add("Home");
                return sa;
            }
        }

        public string Action(string actionName, string actionParameters)
        {
            if (actionName == "Home")
            {
                CommandString("H#", false);
                return "";
            }
            throw new ASCOM.ActionNotImplementedException("Action " + actionName + " is not implemented by this driver");
        }

        public void CommandBlind(string command, bool raw)
        {
            CheckConnected("CommandBlind");
            this.CommandString(command, raw);
        }

        public bool CommandBool(string command, bool raw)
        {
            CheckConnected("CommandBool");
            string ret = CommandString(command, raw);
            return ret.IndexOf("true", StringComparison.OrdinalIgnoreCase) >= 0 || ret.Trim().StartsWith("1");
        }

        public string CommandString(string command, bool raw)
        {
            CheckConnected("CommandString");
            if (!this.Connected || connection == null)
                throw new ASCOM.NotConnectedException();

            string temp = "999";
            mutex.WaitOne();
            try
            {
                tl.LogMessage("Sending Command: ", command);
                temp = connection.CommandString(command);
                tl.LogMessage("Got Response: ", temp);
            }
            catch (Exception e)
            {
                tl.LogMessage("Caught exception in CommandString ", e.Message);
                throw new ASCOM.DriverException("Focuser command failed.", e);
            }
            finally
            {
                mutex.ReleaseMutex();
            }
            return temp;
        }

        public void Dispose()
        {
            tl.Enabled = false;
            tl.Dispose();
            tl = null;
            utilities.Dispose();
            utilities = null;
            astroUtilities.Dispose();
            astroUtilities = null;

            if (connection == null) return;
            connection.Dispose();
            connection = null;
        }

        public bool Connected
        {
            get
            {
                tl.LogMessage("Connected Get", IsConnected.ToString());
                return IsConnected;
            }
            set
            {
                tl.LogMessage("Connected Set", value.ToString());
                if (value == IsConnected) return;

                if (value)
                {
                    bool contHold = false;
                    if (connection != null && connection.IsConnected) return;

                    using (ASCOM.Utilities.Profile p = new Profile())
                    {
                        p.DeviceType = "Focuser";
                        if (!p.IsRegistered(driverID))
                        {
                            p.Register(driverID, driverDescription);
                        }

                        transport = GetProfileValue(p, transportProfileName, transportDefault);
                        comPort = GetProfileValue(p, comPortLegacyProfileName, GetProfileValue(p, comPortProfileName, comPortDefault));
                        tcpHost = GetProfileValue(p, tcpHostProfileName, tcpHostDefault);
                        tcpPort = ParseInt(GetProfileValue(p, tcpPortProfileName, tcpPortDefault), 4030);
                        commandTimeoutMs = ParseInt(GetProfileValue(p, commandTimeoutProfileName, commandTimeoutDefault), 3000);
                        contHold = GetProfileValue(p, "ContHold", "false").ToLowerInvariant().Equals("true");
                        maxStep = ParseInt(GetProfileValue(p, "MaxStep", "20000"), 20000);

                        try
                        {
                            connection = CreateConnection();
                            tl.LogMessage("Connecting to focuser", connection.EndpointDescription);
                            connection.Connect();
                            connectedState = true;
                            lastLink = true;

                            ValidateDeviceIdentity();

                            // Send maxStep to firmware
                            if (IsTcpTransport())
                                CommandString("D " + maxStep.ToString() + "#", false);

                            if (contHold)
                                CommandString("C 1#", false);
                            else
                                CommandString("C 0#", false);

                            string ver = CommandString("V#", false);
                            string verTrim = ver.Replace('#', ' ').Trim();
                            string versn = verTrim.Replace('V', ' ').Trim();
                            tl.LogMessage("Firmware Version", versn);
                        }
                        catch (Exception ex)
                        {
                            connectedState = false;
                            lastLink = false;
                            if (connection != null)
                            {
                                connection.Dispose();
                                connection = null;
                            }
                            throw new ASCOM.NotConnectedException("Focuser connection error", ex);
                        }
                    }
                }
                else
                {
                    try
                    {
                        CommandString("C 0#", false);
                    }
                    catch (Exception ex)
                    {
                        tl.LogMessage("Disconnect hold release failed", ex.Message);
                    }
                    System.Threading.Thread.Sleep(500);
                    connectedState = false;
                    lastLink = false;
                    if (connection != null)
                    {
                        tl.LogMessage("Connected Set", "Disconnecting from " + connection.EndpointDescription);
                        connection.Dispose();
                        connection = null;
                    }
                }
            }
        }

        public string Description
        {
            get
            {
                tl.LogMessage("Description Get", driverDescription);
                return driverDescription;
            }
        }

        public string DriverInfo
        {
            get
            {
                Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                string driverInfo = "EFucoser ESP8266 Focuser Driver. Version: " + String.Format(CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor);
                tl.LogMessage("DriverInfo Get", driverInfo);
                return driverInfo;
            }
        }

        public string DriverVersion
        {
            get
            {
                Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                string driverVersion = String.Format(CultureInfo.InvariantCulture, "{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);
                tl.LogMessage("DriverVersion Get", driverVersion);
                return driverVersion;
            }
        }

        public short InterfaceVersion
        {
            get
            {
                tl.LogMessage("InterfaceVersion Get", "3");
                return 3;
            }
        }

        public string Name
        {
            get
            {
                string name = "EFucoser ESP8266 Focuser";
                tl.LogMessage("Name Get", name);
                return name;
            }
        }

        // ==================== IFocuserV3 Specific ====================

        public bool Absolute
        {
            get
            {
                tl.LogMessage("Absolute Get", true.ToString());
                return true;
            }
        }

        public bool IsMoving
        {
            get
            {
                DoUpdate();
                return lastMoving;
            }
        }

        public bool Link
        {
            get
            {
                long now = DateTime.Now.Ticks;
                if (now - lastL > UPDATETICKS)
                {
                    if (connection != null)
                        lastLink = connection.IsConnected;
                    lastL = now;
                    return lastLink;
                }
                return lastLink;
            }
            set
            {
                this.Connected = value;
            }
        }

        public int MaxIncrement
        {
            get
            {
                tl.LogMessage("MaxIncrement Get", maxStep.ToString());
                return maxStep;
            }
        }

        public int MaxStep
        {
            get
            {
                tl.LogMessage("MaxStep Get", maxStep.ToString());
                return maxStep;
            }
        }

        public int Position
        {
            get
            {
                DoUpdate();
                return Math.Max(0, Math.Min(lastPos, maxStep));
            }
        }

        public double StepSize
        {
            get
            {
                tl.LogMessage("StepSize Get", stepSize.ToString());
                return (double)stepSize;
            }
        }

        public bool TempComp
        {
            get
            {
                tl.LogMessage("TempComp Get", false.ToString());
                return false;
            }
            set
            {
                tl.LogMessage("TempComp Set Ignored", value.ToString());
            }
        }

        public bool TempCompAvailable
        {
            get
            {
                tl.LogMessage("TempCompAvailable Get", false.ToString());
                return false;
            }
        }

        public double Temperature
        {
            get
            {
                try
                {
                    string resp = CommandString("I#", false);
                    // Extract lastTemp from JSON
                    var match = System.Text.RegularExpressions.Regex.Match(resp, "\"lastTemp\":(-?[0-9]+(?:\\.[0-9]+)?)");
                    if (match.Success)
                    {
                        double temp;
                        if (double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out temp))
                        {
                            lastTemp = temp;
                            return temp;
                        }
                    }
                }
                catch { }
                return lastTemp;
            }
        }

        public void Halt()
        {
            CommandString("S#", false);
            tl.LogMessage("Halt", "Stopped");
        }

        public void Move(int position)
        {
            if (position < 0) position = 0;
            if (position > maxStep) position = maxStep;

            CommandString("M " + position.ToString() + "#", false);
            lastMoving = true;
            tl.LogMessage("Move", position.ToString());
        }

        public void TemperatureSet(double temperature)
        {
            int tempInt = (int)Math.Round(temperature * 100.0);
            CommandString("E " + tempInt.ToString() + "#", false);
            tl.LogMessage("TemperatureSet", temperature.ToString("F2"));
        }

        // ==================== Internal Helpers ====================

        private void DoUpdate()
        {
            if (DateTime.Now.Ticks > UPDATETICKS + lastUpdate)
            {
                lastUpdate = DateTime.Now.Ticks;
                try
                {
                    string val = CommandString("G#", false);
                    // Response: "P <steps>;M <true|false>#"
                    string[] vals = val.Replace('#', ' ').Trim().Split(';');
                    if (vals.Length >= 2)
                    {
                        string valTrim = vals[0].Replace('#', ' ');
                        string pos = valTrim.Replace('P', ' ').Trim();
                        lastPos = Convert.ToInt32(pos);
                        lastMoving = vals[1].Substring(2) == "true";
                    }
                }
                catch (Exception ex)
                {
                    tl.LogMessage("DoUpdate error", ex.Message);
                }
            }
        }

        private bool IsTcpTransport()
        {
            return string.Equals(transport, "TCP", StringComparison.OrdinalIgnoreCase)
                || string.Equals(transport, "WiFi TCP", StringComparison.OrdinalIgnoreCase);
        }

        private IFocuserConnection CreateConnection()
        {
            if (IsTcpTransport())
            {
                if (string.IsNullOrWhiteSpace(tcpHost))
                    throw new ASCOM.NotConnectedException("No TCP host selected");
                return new TcpFocuserConnection(tcpHost, tcpPort, commandTimeoutMs);
            }

            if (string.IsNullOrWhiteSpace(comPort))
                throw new ASCOM.NotConnectedException("No COM port selected");
            return new SerialFocuserConnection(comPort);
        }

        private void ValidateDeviceIdentity()
        {
            string identity = CommandString("#", false).Replace('#', ' ').Trim();
            tl.LogMessage("Device Identity", identity);

            if (IsExpectedFocuserIdentity(identity))
                return;

            throw new ASCOM.NotConnectedException(
                "Connected device is not an EFucoser focuser. Response: " + identity);
        }

        internal static bool IsExpectedFocuserIdentity(string identity)
        {
            if (string.IsNullOrWhiteSpace(identity))
                return false;

            return IsExpectedIdentityPrefix(identity, Esp8266FocuserIdentity)
                || IsExpectedIdentityPrefix(identity, Esp8266Uln2003FocuserIdentity)
                || IsExpectedIdentityPrefix(identity, ArduinoNanoUln2003FocuserIdentity);
        }

        private static bool IsExpectedIdentityPrefix(string identity, string expectedPrefix)
        {
            return string.Equals(identity, expectedPrefix, StringComparison.Ordinal)
                || identity.StartsWith(expectedPrefix + " ver", StringComparison.Ordinal);
        }

        internal static int ParseInt(string value, int defaultValue)
        {
            int parsed;
            if (int.TryParse(value, out parsed)) return parsed;
            return defaultValue;
        }

        internal static string GetProfileValue(Profile profile, string name, string defaultValue)
        {
            string value = profile.GetValue(driverID, name, string.Empty, defaultValue);
            if (string.IsNullOrWhiteSpace(value)) return defaultValue;
            return value;
        }

        internal void ReadProfile()
        {
            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "Focuser";
                traceState = Convert.ToBoolean(GetProfileValue(driverProfile, traceStateProfileName, traceStateDefault));
                comPort = GetProfileValue(driverProfile, comPortLegacyProfileName, GetProfileValue(driverProfile, comPortProfileName, comPortDefault));
                transport = GetProfileValue(driverProfile, transportProfileName, transportDefault);
                tcpHost = GetProfileValue(driverProfile, tcpHostProfileName, tcpHostDefault);
                tcpPort = ParseInt(GetProfileValue(driverProfile, tcpPortProfileName, tcpPortDefault), 4030);
                commandTimeoutMs = ParseInt(GetProfileValue(driverProfile, commandTimeoutProfileName, commandTimeoutDefault), 3000);
                maxStep = ParseInt(GetProfileValue(driverProfile, "MaxStep", "20000"), 20000);
            }
        }

        internal void WriteProfile()
        {
            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "Focuser";
                driverProfile.WriteValue(driverID, traceStateProfileName, traceState.ToString());
                driverProfile.WriteValue(driverID, comPortProfileName, comPort.ToString());
                driverProfile.WriteValue(driverID, comPortLegacyProfileName, comPort.ToString());
                driverProfile.WriteValue(driverID, transportProfileName, transport.ToString());
                driverProfile.WriteValue(driverID, tcpHostProfileName, tcpHost.ToString());
                driverProfile.WriteValue(driverID, tcpPortProfileName, tcpPort.ToString());
                driverProfile.WriteValue(driverID, commandTimeoutProfileName, commandTimeoutMs.ToString());
                driverProfile.WriteValue(driverID, "MaxStep", maxStep.ToString());
            }
        }

        #region ASCOM Registration

        private static void RegUnregASCOM(bool bRegister)
        {
            using (var P = new ASCOM.Utilities.Profile())
            {
                P.DeviceType = "Focuser";
                if (bRegister)
                    P.Register(driverID, driverDescription);
                else
                    P.Unregister(driverID);
            }
        }

        [ComRegisterFunction]
        public static void RegisterASCOM(Type t)
        {
            RegUnregASCOM(true);
        }

        [ComUnregisterFunction]
        public static void UnregisterASCOM(Type t)
        {
            RegUnregASCOM(false);
        }

        #endregion

        private bool IsConnected
        {
            get { return connectedState; }
        }

        private void CheckConnected(string message)
        {
            if (!IsConnected)
                throw new ASCOM.NotConnectedException(message);
        }
    }
}
