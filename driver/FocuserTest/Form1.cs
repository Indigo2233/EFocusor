using System;
using System.Drawing;
using System.Windows.Forms;
using ASCOM.DriverAccess;

namespace ASCOM.EFucoser.Test
{
    public partial class Form1 : Form
    {
        private Focuser focuser;
        private Timer updateTimer;

        private Button btnChoose;
        private Button btnConnect;
        private Button btnDisconnect;
        private Button btnMove;
        private Button btnHalt;
        private Button btnHome;
        private TextBox txtPosition;
        private TextBox txtTarget;
        private TextBox txtMaxStep;
        private Label lblStatus;
        private Label lblPosition;
        private Label lblMoving;
        private Label lblTemp;
        private NumericUpDown nudTarget;

        public Form1()
        {
            InitializeComponent();
            updateTimer = new Timer();
            updateTimer.Interval = 500;
            updateTimer.Tick += UpdateTimer_Tick;
        }

        private void InitializeComponent()
        {
            this.Text = "EFucoser Focuser Test";
            this.Size = new Size(500, 450);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(17, 19, 24);
            this.ForeColor = Color.FromArgb(242, 245, 248);

            int y = 20;
            int leftX = 20;
            int rightX = 260;

            // Status
            lblStatus = new Label { Location = new Point(leftX, y), AutoSize = true, Text = "Status: Disconnected" };
            this.Controls.Add(lblStatus);

            // Choose
            btnChoose = CreateButton("Choose Focuser", leftX, y += 30);
            btnChoose.Click += BtnChoose_Click;
            this.Controls.Add(btnChoose);

            // Connect / Disconnect
            btnConnect = CreateButton("Connect", leftX, y += 40);
            btnConnect.Click += BtnConnect_Click;
            this.Controls.Add(btnConnect);

            btnDisconnect = CreateButton("Disconnect", rightX, y);
            btnDisconnect.Click += BtnDisconnect_Click;
            this.Controls.Add(btnDisconnect);

            // Position display
            lblPosition = new Label { Location = new Point(leftX, y += 40), AutoSize = true, Text = "Position: --" };
            this.Controls.Add(lblPosition);

            lblMoving = new Label { Location = new Point(leftX, y += 25), AutoSize = true, Text = "Moving: --" };
            this.Controls.Add(lblMoving);

            lblTemp = new Label { Location = new Point(leftX, y += 25), AutoSize = true, Text = "Temperature: --" };
            this.Controls.Add(lblTemp);

            txtMaxStep = new Label { Location = new Point(leftX, y += 25), AutoSize = true, Text = "MaxStep: --" };
            this.Controls.Add(txtMaxStep);

            // Target
            var lblTarget = new Label { Location = new Point(leftX, y += 40), AutoSize = true, Text = "Target Position:" };
            this.Controls.Add(lblTarget);

            nudTarget = new NumericUpDown
            {
                Location = new Point(leftX, y += 25),
                Size = new Size(200, 22),
                Minimum = 0,
                Maximum = 100000,
                Value = 0,
                BackColor = Color.FromArgb(18, 23, 32),
                ForeColor = Color.FromArgb(242, 245, 248)
            };
            this.Controls.Add(nudTarget);

            // Move
            btnMove = CreateButton("Move", leftX, y += 40);
            btnMove.Click += BtnMove_Click;
            this.Controls.Add(btnMove);

            // Halt
            btnHalt = CreateButton("Halt", rightX, y);
            btnHalt.Click += BtnHalt_Click;
            this.Controls.Add(btnHalt);

            // Home
            btnHome = CreateButton("Home", leftX, y += 40);
            btnHome.Click += BtnHome_Click;
            this.Controls.Add(btnHome);
        }

        private Button CreateButton(string text, int x, int y)
        {
            return new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(200, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(38, 50, 68),
                ForeColor = Color.FromArgb(242, 245, 248)
            };
        }

        private void BtnChoose_Click(object sender, EventArgs e)
        {
            try
            {
                focuser = new Focuser("ASCOM.EFucoser.Focuser");
                lblStatus.Text = "Status: Driver selected";
                btnConnect.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void BtnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                focuser = new Focuser("ASCOM.EFucoser.Focuser");
                focuser.Connected = true;
                lblStatus.Text = "Status: Connected";
                updateTimer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Connect error: " + ex.Message);
            }
        }

        private void BtnDisconnect_Click(object sender, EventArgs e)
        {
            try
            {
                updateTimer.Stop();
                focuser.Connected = false;
                focuser.Dispose();
                focuser = null;
                lblStatus.Text = "Status: Disconnected";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Disconnect error: " + ex.Message);
            }
        }

        private void BtnMove_Click(object sender, EventArgs e)
        {
            try
            {
                focuser.Move((int)nudTarget.Value);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Move error: " + ex.Message);
            }
        }

        private void BtnHalt_Click(object sender, EventArgs e)
        {
            try
            {
                focuser.Halt();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Halt error: " + ex.Message);
            }
        }

        private void BtnHome_Click(object sender, EventArgs e)
        {
            try
            {
                focuser.Action("Home", "");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Home error: " + ex.Message);
            }
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (focuser != null && focuser.Connected)
                {
                    lblPosition.Text = "Position: " + focuser.Position.ToString();
                    lblMoving.Text = "Moving: " + focuser.IsMoving.ToString();
                    lblTemp.Text = "Temperature: " + focuser.Temperature.ToString("F2") + " °C";
                    txtMaxStep.Text = "MaxStep: " + focuser.MaxStep.ToString();
                }
            }
            catch { }
        }
    }
}
