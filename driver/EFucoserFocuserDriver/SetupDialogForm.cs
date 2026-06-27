using System;
using System.Drawing;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using ASCOM.Utilities;

namespace ASCOM.EFucoser
{
    [ComVisible(false)]
    public partial class SetupDialogForm : Form
    {
        private static readonly Color BackgroundColor = Color.FromArgb(17, 19, 24);
        private static readonly Color PanelAltColor = Color.FromArgb(32, 39, 52);
        private static readonly Color LineColor = Color.FromArgb(56, 66, 82);
        private static readonly Color FieldColor = Color.FromArgb(18, 23, 32);
        private static readonly Color TextColor = Color.FromArgb(242, 245, 248);
        private static readonly Color MutedColor = Color.FromArgb(174, 184, 198);
        private static readonly Color AccentColor = Color.FromArgb(77, 182, 172);

        public SetupDialogForm()
        {
            InitializeComponent();
            InitUI();
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            this.BackColor = BackgroundColor;
            this.ForeColor = TextColor;

            foreach (Control c in this.Controls)
            {
                ApplyThemeToControl(c);
            }
        }

        private void ApplyThemeToControl(Control c)
        {
            if (c is Button btn)
            {
                btn.BackColor = Color.FromArgb(38, 50, 68);
                btn.ForeColor = TextColor;
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderColor = LineColor;
                btn.FlatAppearance.BorderSize = 1;
            }
            else if (c is TextBox tb)
            {
                tb.BackColor = FieldColor;
                tb.ForeColor = TextColor;
                tb.BorderStyle = BorderStyle.FixedSingle;
            }
            else if (c is ComboBox cb)
            {
                cb.BackColor = FieldColor;
                cb.ForeColor = TextColor;
                cb.FlatStyle = FlatStyle.Flat;
            }
            else if (c is Label lbl)
            {
                lbl.ForeColor = MutedColor;
            }
            else if (c is CheckBox chk)
            {
                chk.ForeColor = TextColor;
            }
            else if (c is GroupBox gb)
            {
                gb.ForeColor = TextColor;
                foreach (Control gc in gb.Controls)
                    ApplyThemeToControl(gc);
            }
            else if (c is Panel pnl)
            {
                pnl.BackColor = PanelAltColor;
                foreach (Control pc in pnl.Controls)
                    ApplyThemeToControl(pc);
            }
        }

        private void cmdOK_Click(object sender, EventArgs e)
        {
            if (!ValidateInputs())
            {
                this.DialogResult = DialogResult.None;
                return;
            }

            using (ASCOM.Utilities.Profile p = new Utilities.Profile())
            {
                p.DeviceType = "Focuser";
                p.WriteValue(Focuser.driverID, Focuser.transportProfileName, comboBoxTransport.Text);
                p.WriteValue(Focuser.driverID, Focuser.comPortLegacyProfileName, comboBoxComPort.Text);
                p.WriteValue(Focuser.driverID, Focuser.comPortProfileName, comboBoxComPort.Text);
                p.WriteValue(Focuser.driverID, Focuser.tcpHostProfileName, textBoxTcpHost.Text.Trim());
                p.WriteValue(Focuser.driverID, Focuser.tcpPortProfileName, textBoxTcpPort.Text.Trim());
                p.WriteValue(Focuser.driverID, Focuser.commandTimeoutProfileName, textBoxTimeout.Text.Trim());
                p.WriteValue(Focuser.driverID, "ContHold", checkBoxHold.Checked.ToString());
                p.WriteValue(Focuser.driverID, "MaxStep", textBoxMaxStep.Text.Trim());
            }

            Focuser.transport = comboBoxTransport.Text;
            Focuser.comPort = comboBoxComPort.Text;
            Focuser.tcpHost = textBoxTcpHost.Text.Trim();
            Focuser.tcpPort = Focuser.ParseInt(textBoxTcpPort.Text.Trim(), 4030);
            Focuser.commandTimeoutMs = Focuser.ParseInt(textBoxTimeout.Text.Trim(), 3000);
            Focuser.traceState = chkTrace.Checked;
            Focuser.maxStep = Focuser.ParseInt(textBoxMaxStep.Text.Trim(), 20000);

            Dispose();
        }

        private void cmdCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void BrowseToAscom(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("http://ascom-standards.org/");
            }
            catch (System.ComponentModel.Win32Exception noBrowser)
            {
                if (noBrowser.ErrorCode == -2147467259)
                    MessageBox.Show(noBrowser.Message);
            }
            catch (System.Exception other)
            {
                MessageBox.Show(other.Message);
            }
        }

        private bool ValidateInputs()
        {
            int numericValue;
            if (string.IsNullOrWhiteSpace(textBoxMaxStep.Text) || !int.TryParse(textBoxMaxStep.Text.Trim(), out numericValue) || numericValue <= 0)
            {
                MessageBox.Show("You must specify a positive value for Max Step");
                return false;
            }
            if (comboBoxTransport.Text == "TCP" && string.IsNullOrWhiteSpace(textBoxTcpHost.Text))
            {
                MessageBox.Show("You must specify a TCP host");
                return false;
            }
            if (!int.TryParse(textBoxTcpPort.Text.Trim(), out numericValue) || numericValue <= 0)
            {
                MessageBox.Show("You must specify a valid TCP port");
                return false;
            }
            if (!int.TryParse(textBoxTimeout.Text.Trim(), out numericValue) || numericValue <= 0)
            {
                MessageBox.Show("You must specify a valid command timeout");
                return false;
            }
            return true;
        }

        private void checkTextBox()
        {
            int numericValue;
            if (string.IsNullOrWhiteSpace(textBoxMaxStep.Text) || !int.TryParse(textBoxMaxStep.Text.Trim(), out numericValue) || numericValue <= 0)
                cmdOK.Enabled = false;
            else if (comboBoxTransport.Text == "TCP" && string.IsNullOrWhiteSpace(textBoxTcpHost.Text))
                cmdOK.Enabled = false;
            else if (!int.TryParse(textBoxTcpPort.Text.Trim(), out numericValue) || numericValue <= 0)
                cmdOK.Enabled = false;
            else if (!int.TryParse(textBoxTimeout.Text.Trim(), out numericValue) || numericValue <= 0)
                cmdOK.Enabled = false;
            else
                cmdOK.Enabled = true;
        }

        private void comboBoxTransport_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateTransportFields();
        }

        private void UpdateTransportFields()
        {
            bool isTcp = comboBoxTransport.Text == "TCP";
            comboBoxComPort.Enabled = !isTcp;
            btnRefreshCom.Enabled = !isTcp;
            btnAutoDetect.Enabled = !isTcp;
            textBoxTcpHost.Enabled = isTcp;
            textBoxTcpPort.Enabled = isTcp;
            checkTextBox();
        }

        private void btnRefreshCom_Click(object sender, EventArgs e)
        {
            RefreshComPorts();
        }

        private void btnAutoDetect_Click(object sender, EventArgs e)
        {
            AutoDetectFocuser();
        }

        private void RefreshComPorts()
        {
            comboBoxComPort.Items.Clear();
            string[] ports = SerialPort.GetPortNames();
            comboBoxComPort.Items.AddRange(ports);
            if (ports.Length > 0 && !comboBoxComPort.Items.Contains(comboBoxComPort.Text))
                comboBoxComPort.SelectedIndex = 0;
            lblDetectStatus.Text = ports.Length > 0
                ? $"Found {ports.Length} COM port(s)"
                : "No COM ports found";
            lblDetectStatus.ForeColor = MutedColor;
        }

        private void AutoDetectFocuser()
        {
            btnAutoDetect.Enabled = false;
            btnRefreshCom.Enabled = false;
            comboBoxComPort.Enabled = false;
            lblDetectStatus.Text = "Scanning...";
            lblDetectStatus.ForeColor = AccentColor;
            Application.DoEvents();

            string[] ports = SerialPort.GetPortNames();
            if (ports.Length == 0)
            {
                lblDetectStatus.Text = "No COM ports to scan";
                lblDetectStatus.ForeColor = Color.FromArgb(238, 107, 99);
                comboBoxComPort.Enabled = true;
                btnRefreshCom.Enabled = true;
                btnAutoDetect.Enabled = true;
                return;
            }

            string foundPort = null;
            string foundVersion = null;

            foreach (string port in ports)
            {
                lblDetectStatus.Text = $"Trying {port}...";
                lblDetectStatus.ForeColor = MutedColor;
                Application.DoEvents();

                try
                {
                    using (var sp = new SerialPort(port, 9600, Parity.None, 8, StopBits.One))
                    {
                        sp.ReadTimeout = 1500;
                        sp.WriteTimeout = 1500;
                        sp.Open();
                        sp.DiscardInBuffer();
                        sp.DiscardOutBuffer();

                        // Send version query - EFucoser responds "V <version>#"
                        sp.Write("V#");
                        System.Threading.Thread.Sleep(300);

                        var sb = new StringBuilder();
                        var deadline = DateTime.Now.AddMilliseconds(1000);
                        while (DateTime.Now < deadline)
                        {
                            try
                            {
                                int b = sp.ReadByte();
                                if (b < 0) break;
                                char c = (char)b;
                                sb.Append(c);
                                if (c == '#') break;
                            }
                            catch (TimeoutException)
                            {
                                break;
                            }
                        }

                        string response = sb.ToString().Trim();
                        if (response.StartsWith("V ", StringComparison.OrdinalIgnoreCase)
                            || response.StartsWith("EFucoser", StringComparison.OrdinalIgnoreCase))
                        {
                            foundPort = port;
                            foundVersion = response.Replace("#", "").Trim();
                            break;
                        }
                    }
                }
                catch (Exception)
                {
                    // Port in use or unavailable - skip
                }
            }

            if (foundPort != null)
            {
                comboBoxComPort.Text = foundPort;
                lblDetectStatus.Text = $"Found: {foundPort} ({foundVersion})";
                lblDetectStatus.ForeColor = Color.FromArgb(123, 201, 111);
            }
            else
            {
                lblDetectStatus.Text = "No EFucoser focuser detected on any COM port";
                lblDetectStatus.ForeColor = Color.FromArgb(238, 107, 99);
            }

            comboBoxComPort.Enabled = true;
            btnRefreshCom.Enabled = true;
            btnAutoDetect.Enabled = true;
        }

        private void InitUI()
        {
            chkTrace.Checked = Focuser.traceState;

            comboBoxTransport.Items.Clear();
            comboBoxTransport.Items.AddRange(new object[] { "TCP", "Serial" });
            // Default to TCP since ESP8266 creates its own WiFi AP
            comboBoxTransport.SelectedItem = string.Equals(Focuser.transport, "Serial", StringComparison.OrdinalIgnoreCase)
                ? "Serial"
                : "TCP";

            RefreshComPorts();

            using (ASCOM.Utilities.Profile p = new Utilities.Profile())
            {
                p.DeviceType = "Focuser";
                textBoxTcpHost.Text = Focuser.GetProfileValue(p, Focuser.tcpHostProfileName, Focuser.tcpHostDefault);
                textBoxTcpPort.Text = Focuser.GetProfileValue(p, Focuser.tcpPortProfileName, Focuser.tcpPortDefault);
                textBoxTimeout.Text = Focuser.GetProfileValue(p, Focuser.commandTimeoutProfileName, Focuser.commandTimeoutDefault);
                textBoxMaxStep.Text = Focuser.GetProfileValue(p, "MaxStep", "20000");

                if (p.GetValue(Focuser.driverID, "ContHold") == "True")
                    checkBoxHold.Checked = true;
                else
                    checkBoxHold.Checked = false;
            }

            UpdateTransportFields();
        }
    }
}
