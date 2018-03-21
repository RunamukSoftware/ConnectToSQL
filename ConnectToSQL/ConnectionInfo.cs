using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Security.Principal;
using System.Windows.Forms;

namespace ToolManTaylor
{
    public partial class ConnectToSQLBase : Form
    {
        #region Properties
        public SplashScreen ss;
        public int ConnectionTimeout { get; set; }
        public int ExecutionTimeout { get; set; }
        public WindowsIdentity Id { get; set; }

        public int MinimumVersion { get; set; }
        public int AuthMethod {get; set; }
        public string ServerName { get; set; }

        #endregion Properties

        #region Form Methods
        public ConnectToSQLBase()
        {
            // Standard Form code
            InitializeComponent();
            // Let's capture this for Azure or Windows Auth
            Id = WindowsIdentity.GetCurrent();
            txtUserName.Text = Id.Name;
            cbAuthentication.SelectedIndex = 0;
            cbServer.SelectedIndex = 0;
            MinimumVersion = 12;

        }
        #endregion Form Methods

        #region Event Handlers

        protected void btnCancel_Click(object sender, EventArgs e)
        {
            // User choose not to run
            this.Close();
        }

        protected void btnConnect_Click(object sender, EventArgs e)
        {
            Cursor current = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;
            // Create our connection information for use by the Results Form
            ServerConnection sc = new ServerConnection(ServerName);
            // Create a ConnectToSQLBase object to simplify management
            SqlConnectionInfo sci = new SqlConnectionInfo(sc, ConnectionType.Sql);
            // If we are using Azure Password Authentication
            sci.Authentication = SqlConnectionInfo.AuthenticationMethod.NotSpecified;
            switch (AuthMethod)
            {
                case 0: // Windows Auth
                    sci.UseIntegratedSecurity = true;
                    break;
                case 1: // SQL Server Login
                    sci.UserName = txtUserName.Text;
                    sci.Password = txtPassword.Text;
                    break;
                case 2: // Active Directory Password Authentication
                    sci.Authentication = SqlConnectionInfo.AuthenticationMethod.ActiveDirectoryPassword;
                    sci.UserName = txtUserName.Text;
                    sci.Password = txtPassword.Text;
                    sci.EncryptConnection = true;
                    break;
                case 3: // Active Directory Integrated Authentication
                    sci.Authentication = SqlConnectionInfo.AuthenticationMethod.ActiveDirectoryIntegrated;
                    sci.UseIntegratedSecurity = true;
                    sci.UserName = Id.Name;
                    sci.EncryptConnection = true;
                    break;
            }
            // Use TCP connection
            sci.ConnectionProtocol = NetworkProtocol.TcpIp;
            // Set user requested timeout
            sci.ConnectionTimeout = ConnectionTimeout;
            // Finally, we can create our SqlConnection         
            SqlConnection con = new SqlConnection(sci.ConnectionString);
            try
            {
                // Now, make sure we can open a connection (to ensure user has rights)
                // if they don't have rights, it will throw an exception
                DoWork(con, sci);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.Source, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor.Current = current;
                con.Close();
            }
        }

        public virtual void DoWork(SqlConnection con, SqlConnectionInfo sci)
        {
            //con.Open();
            //SqlCommand cmd = new SqlCommand("SELECT SERVERPROPERTY('ProductVersion')", con);
            //string verison = (string)cmd.ExecuteScalar();
            //con.Close();
            //string[] verParts = verison.Split('.');

            //// Must be SQL Azure Database or SQL Server 2016 or later
            //if ((Convert.ToInt32(verParts[0]) >= 12 && sci.Authentication == SqlConnectionInfo.AuthenticationMethod.ActiveDirectoryIntegrated) ||
            //    (Convert.ToInt32(verParts[0]) >= 12 && sci.Authentication == SqlConnectionInfo.AuthenticationMethod.ActiveDirectoryPassword) ||
            //        (Convert.ToInt32(verParts[0]) >= 13 && sci.Authentication == SqlConnectionInfo.AuthenticationMethod.NotSpecified))
            //{
            //    // Now present the main screen to the user for selection of database and processing options
            //    //Results r = new Results(con);
            //    //r.ConnectToSQLBase = sci;
            //    //r.ConnectionTimeout = ConnectionTimeout;
            //    //r.ExecutionTimeout = ExecutionTimeout;
            //    //r.ShowDialog();
            //    //this.Close();
            //}
            //else
            //{
            //    MessageBox.Show("Temporal Data Capture is only available on SQL Azure Database or SQL Server 2016 or later.", "Temporal Data Capture");
            //}
        }

        private void cbAuthentication_SelectedIndexChanged(object sender, EventArgs e)
        {
            // As the user changes authentication method, enable / disable the Username and Password fields 
            ComboBox cb = (ComboBox)sender;
            AuthMethod = cb.SelectedIndex;
            if (cb.SelectedIndex == 1 || cb.SelectedIndex == 2)
            {
                txtUserName.Enabled = true;
                txtPassword.Enabled = true;
                txtUserName.Text = "";
                txtPassword.Text = "";
            }
            else
            {
                txtUserName.Enabled = false;
                txtPassword.Enabled = false;
                txtUserName.Text = Id.Name;
            }
        }

        private void cbServer_TextChanged(object sender, EventArgs e)
        {
            ComboBox cb = (ComboBox)sender;
            ServerName = cb.Text;
        }

        private void cbServer_SelectedIndexChanged(object sender, EventArgs e)
        {
            //string selection = ((ComboBox)sender).SelectedItem.ToString();
            ComboBox cb = (ComboBox)sender;
            if (cb.SelectedItem != null)
            {
                ServerName = cb.SelectedItem.ToString();
            }
            if (!string.IsNullOrEmpty(ServerName) && ServerName == "<Browse for more...>")
            {
                FillServerList();
            }
        }

        private void cbServer_SelectionChangeCommitted(object sender, EventArgs e)
        {
            ComboBox cb = (ComboBox)sender;
            if (cb.SelectedItem != null)
            {
                ServerName = cb.SelectedItem.ToString();
            }
            if (!string.IsNullOrEmpty(ServerName) && ServerName == "<Browse for more...>")
            {
                FillServerList();
            }
        }

        private void nudConnectionTimeOut_ValueChanged(object sender, EventArgs e)
        {
            ConnectionTimeout = Convert.ToInt32(nudConnectionTimeOut.Value);
        }

        private void nudExecutionTimeout_ValueChanged(object sender, EventArgs e)
        {
            ExecutionTimeout = Convert.ToInt32(nudExecutionTimeout.Value);
        }
       
        #endregion Event Handlers

        #region Helper Functions
        private void FillServerList()
        {
            //When the user has asked to load the server list display a message
            // and  set the location of the splash screen to our location and move it to the foreground
            // display message to user so they know why there is a delay
            ss = new SplashScreen();
            ss.Show();
            ss.Location = this.Location;
            ss.BringToFront();

            // Allow the form to display (behind the splash screen)
            this.Show();

            // Make sure the UI is repsonsive
            Application.DoEvents();

            // Save the current cursor and display a wait cursor during this operation
            Cursor current = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;

            // Use the SmoApplication to get a list of SQL Servers
            // Need to decide if this is a configuration item - only use local servers
            bool useLocalServers = true;
            DataTable dt = SmoApplication.EnumAvailableSqlServers(useLocalServers);
            cbServer.Items.Clear();
            cbServer.Items.Add("(local)");
            // Work item 311
            // Only add servers that are at the minimum version or greater
            foreach (DataRow r in dt.Rows)
            {
                string[] verParts = r["Version"].ToString().Split('.');
                // Only add servers that are at the minimum version or greater
                if (Convert.ToInt32(verParts[0]) >= MinimumVersion)
                {
                    cbServer.Items.Add(r[0].ToString());
                }
            }
            cbServer.Items.Add("<Browse for more...>");

            // Get rid of the Splash Screen
            ss.Close();

            // And restor the users cursor
            Cursor.Current = current;
        }
        #endregion Helper Functions


    }
}
