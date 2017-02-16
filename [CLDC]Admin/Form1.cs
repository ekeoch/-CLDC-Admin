using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace _CLDC_Admin
{
    public partial class Form1 : Form
    {
        #region CONSTRUCTOR
        public Form1()
        {
            //Handle proper application exit
            Application.ApplicationExit += new EventHandler(Application_ApplicationExit);
            InitializeComponent();
            Initialize_content();
        }
        #endregion

        #region VARIABLES
        private string UserName
        {
            get
            {
                IPHostEntry My_Host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (IPAddress add in My_Host.AddressList)
                {
                    if (add.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        //Append host ipaddress to Admin
                        return "Admin[" + add.ToString() + "]";
                    }
                }
                //append host name instead
                return "Admin[" + Dns.GetHostName() + "]";
            }
        }
        private StreamWriter swSender;
        private StreamReader srReceiver;
        private TcpClient tcpServer;
        private delegate void UpdateLogCallback(string strMessage);
        private delegate void CloseConnectionCallback(string strReason);
        private Thread thrMessaging;
        private IPAddress ipAddress;
        private bool connected = false;
        private string Host_Ip
        {
            get
            {
                try
                {
                    return comboBox1.Text;
                }
                catch { return string.Empty; }
            }
        }
        private string Dir
        {
            get
            {
                return Environment.CurrentDirectory + @"/" + UserName;
            }
        }//Current directory
        private string filename
        {
            get
            {
                return Dir + @"/" + DateTime.Now.Day + DateTime.Now.Year + ".txt";
            }
        }//Save file name
        private StreamWriter save_stream;
        private bool copying = false;
        private string Client_;
        private string Filetosend_;
        private string ClientN_;
        private string Parameter_;
        #endregion

        #region FORM UTILITY
        /// <summary>
        /// handle Application Exits properly
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Application_ApplicationExit(object sender, EventArgs e)
        {
            //throw new NotImplementedException();
            System.Environment.Exit(System.Environment.ExitCode);

        }

        /// <summary>
        /// Initialize the contents of the form
        /// </summary>
        private void Initialize_content()
        {
            this.Text = "[CLDC]Admin  " + UserName;
            if (!connected)
            {
                //Disable the appriopriate form elements when program starts
                Disable_form();
            }

            Directory.CreateDirectory(Dir);
            if (!File.Exists(filename))
            {
                save_stream = File.CreateText(filename);
                save_stream.Close();
            }
            return;
        }

        /// <summary>
        /// Disable the Elements in the form
        /// </summary>
        private void Disable_form()
        {
            comboBox2.Enabled = false;
            comboBox3.Enabled = false;
            textBox1.Enabled = false;
            textBox2.Enabled = false;
            button2.Enabled = false;
            button3.Enabled = false;
            button4.Enabled = false;
            comboBox1.Enabled = true;
            comboBox2.SelectedIndex = -1;
            comboBox3.SelectedIndex = -1;
            textBox1.Text = null;
            button1.Text = "Connect";
            connected = false;
        }

        /// <summary>
        /// Enable the Elements of the form
        /// </summary>
        private void Enable_form()
        {
            comboBox2.Enabled = true;
            comboBox3.Enabled = true;
            textBox1.Enabled = true;
            textBox2.Enabled = true;
            button2.Enabled = true;
            button3.Enabled = true;
            button4.Enabled = true;
            comboBox1.Enabled = false;
            button1.Text = "Disconnect";
        }

        /// <summary>
        /// Send button functionality
        /// </summary>
        private void button4_Click(object sender, EventArgs e)
        {
            if (textBox2.Text != string.Empty)
            {
                string message = textBox2.Text;
                SendMessage(message);
                UpdateLog("0" + message);
                textBox2.Text = string.Empty;
            }
        }

        /// <summary>
        /// Connect and Disconnect functionality
        /// </summary>
        private void button1_Click(object sender, EventArgs e)
        {
            if (!connected)
            {
                Initialize_connection();
            }
            else
            {
                CloseConnection("Disconnecting from server");
                Disable_form();
                string messg = "[" + DateTime.Now.ToLocalTime() + "] " + "Diconnected from Server at [" + Host_Ip + "]\r\n";
                Post("6" + messg);
                try
                {
                    save_stream = File.AppendText(filename);
                    save_stream.Write(messg);
                    save_stream.Close();
                }
                catch (Exception)
                { }
                comboBox2.Items.Clear();
                comboBox2.Items.Add("Global");
            }
        }

        /// <summary>
        ///  On enter button pressed, send message
        ///  This is for the input textbox
        /// </summary>
        private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (textBox2.Text != string.Empty && e.KeyChar == (char)Keys.Enter)
            {
                SendMessage(textBox2.Text);
                UpdateLog("0" + textBox2.Text);
                textBox2.Text = string.Empty;
            }
        }

        /// <summary>
        ///  Run button functionality
        /// TODO:for local files selected, copy the file to the required Ip address, and delete after it has been run
        /// </summary>
        private void button3_Click(object sender, EventArgs e)
        {
            string message;
            if (comboBox2.SelectedItem == null)
            {
                MessageBox.Show("No Target machine Selected!", "[CLDC]Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            else if (comboBox3.Text == string.Empty)
            {
                MessageBox.Show("Command not defined!", "[CLDC]Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            else if (checkBox1.Checked)
            {
                if(!copying)
                {
                //Get Ip
                string Client = "text";
                string[] Dirs_ = comboBox3.Text.Split('\\');
                Client = comboBox2.Text.Remove(0, comboBox2.Text.IndexOf('[') + 1);
                Client = Client.Remove(Client.Length - 1);
                Client_ = Client;
                Filetosend_ = comboBox3.Text;
                ClientN_ = comboBox2.Text;
                Parameter_ = textBox1.Text;
                UpdateLog("6Copying "+ Dirs_[Dirs_.Length -1] + " to target machine. . .");
                Thread.Sleep(300);
                Thread FileSend = new Thread(new ThreadStart(StartThread));
                FileSend.Start();
                }
            }
            else
            {
                try
                {
                    //To be able to distinguish the command from the parameter since they are all merged into one string,
                    //the lenght of the command string is taken note of and stored in the string
                    message = "@" + comboBox2.Text + " [" + comboBox3.Text.Length + "]" + comboBox3.Text + " ";
                    if (textBox1.Text != string.Empty)
                    {
                        message = message + " " + textBox1.Text;
                    }
                    SendMessage(message);
                    //reset the form to what it was
                    UpdateLog("9" + message);
                    comboBox2.SelectedIndex = -1;
                    comboBox3.SelectedIndex = -1;
                    comboBox3.Text = string.Empty;
                    textBox1.Text = string.Empty;
                }
                catch (Exception Except)
                {
                    //Update the richtextbox with any errors that show up
                    UpdateLog("4" + Except.Message);
                }
            }
        }

        /// <summary>
        /// This represents the browse button functionality
        /// </summary>
        private void button2_Click(object sender, EventArgs e)
        {
            //Disable the button so that n0bs dnt over click it
            button2.Enabled = false;

            OpenFileDialog My_dialog = new OpenFileDialog();
            My_dialog.ShowDialog();

            //send the required file name to the text box
            comboBox3.Text = My_dialog.FileName;
            button2.Enabled = true;
        }

        /// <summary>
        /// save to a log file
        /// </summary>
        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            //string Dir = Environment.CurrentDirectory + @"/" + UserName + "(" + DateTime.Now.Date + ")";
            //string filename = Dir + @"/" + DateTime.Now.Date + ".txt";
            //Directory.CreateDirectory(Dir);
        }
        #endregion

        #region CONNECTION UTILITY
        /// <summary>
        /// Initialize connection to the server
        /// </summary>
        private void Initialize_connection()
        {
            if (connected)
            { return; }//Safe guard to prevent double execution of this method;

            try
            {
                //parse the Ip address in the combo box
                ipAddress = IPAddress.Parse(comboBox1.Text);
            }
            catch (Exception Except)
            {
                //return a messagebox with the exception if the proccess doesnt go right
                MessageBox.Show(Except.Message, "[CLDC]Client Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            tcpServer = new TcpClient();

            //so I am using 20500, if it sticks, change it to something else xD
            try
            {
                tcpServer.Connect(ipAddress, 20500);
            }
            catch (Exception Except)
            {
                //return a messagebox with the exception if the proccess doesnt go right
                MessageBox.Show(Except.Message, "[CLDC]Client Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            this.connected = true;
            Enable_form(); //enable the form upon connection to the server.
            // Send the desired username to the server
            swSender = new StreamWriter(tcpServer.GetStream());
            swSender.WriteLine(UserName);
            swSender.Flush();
            thrMessaging = new Thread(new ThreadStart(ReceiveMessages));
            thrMessaging.Start();
        }

        /// <summary>
        /// method to folow the thrmesaging thread
        /// </summary>
        private void ReceiveMessages()
        {
            //Recieve the Response from the Server
            srReceiver = new StreamReader(tcpServer.GetStream());

            //if the first character of the response is 1, connection was successfull
            //tweak this to account for the possibility the user might be busy/hidden/Or Whatever

            string ConResponse = srReceiver.ReadLine();
            if (ConResponse != null && ConResponse[0] == '1')//Added stuff
            {
                this.Invoke(new UpdateLogCallback(this.UpdateLog), new object[] { "6Connected Successfully!" });
                SendMessage("x!Online");
                //do somthing with the initial connection
            }
            else//The connection was probably Unsuccessful
            {
                if (ConResponse == null)//Added this
                { ConResponse = "00NULL Exception Error!"; }//Added 00 since the first 2 characters are deleted

                string Reason = "Not Connected: ";
                // Extract the reason out of the response message. The reason starts at the 3rd character
                Reason += ConResponse.Substring(2, ConResponse.Length - 2);
                // Update the form with the reason why we couldn't connect

                this.Invoke(new CloseConnectionCallback(this.CloseConnection), new object[] { Reason });

                connected = false;
                // Exit the method
                return;
            }

            //While We are connected to the server Getting Incoming Messages
            while (connected)
            {
                try//Added this
                {
                    this.Invoke(new UpdateLogCallback(this.UpdateLog), new object[] { srReceiver.ReadLine() });
                }// Show the messages in the log TextBox
                catch
                {
                    //if(connected)
                    //{
                    //connected = false;
                    //string messg = "[" + DateTime.Now.ToLocalTime() + "] " + "Diconnected from Server at [" + Host_Ip + "]\r\n";                   
                    //CloseConnection("Disconnecting from server");
                    //this.Disable_form();
                    ////richTextBox1.AppendText(messg);
                    ////Update the log
                    //Post("6" + messg);
                    //try
                    //{
                    //    save_stream = File.AppendText(filename);
                    //    save_stream.Write(messg);
                    //    save_stream.Close();
                    //}
                    //catch (Exception)
                    //{ }
                    //comboBox2.Items.Clear();
                    //comboBox2.Items.Add("Global");
                    //}
                }
            }

        }

        /// <summary>
        /// Update log
        /// conditional parsing happens here
        /// parse the various chat responses that come in
        /// 1 and 0 reserved for connection messages[0 can be overriden however]
        /// simplemessage = 2,
        /// Comboboxinfo = 3,
        /// Errormessage = 4,
        /// encodedmessage = 5,
        /// adminmessage = 6,
        /// newentry = 7,
        /// noobleaver = 8,
        /// command = 9,
        /// updatelog = 0,
        /// </summary>
        public void UpdateLog(string strMessage)
        {
            string str = strMessage.Remove(1, strMessage.Length - 1);
            //messages beginning with 7 8 and 3 are mesasges used to
            //fill in the value =s of the combo box for online and offline users
            if (strMessage.StartsWith("7"))
            {
                strMessage = strMessage.Remove(0, 1);
                string[] content = strMessage.Split();
                if (!comboBox2.Items.Contains((object)content[0]))
                {
                    comboBox2.Items.Add((object)content[0]);
                }
            }
            else if (strMessage.StartsWith("8"))
            {
                strMessage = strMessage.Remove(0, 1);
                string[] content = strMessage.Split();
                if (comboBox2.Items.Contains((object)content[0]))
                {
                    comboBox2.Items.Remove((object)content[0]);
                }
            }
            else if (strMessage.StartsWith("3"))
            {
                strMessage = strMessage.Remove(0, 1);
                if (!comboBox2.Items.Contains((object)strMessage))
                {
                    comboBox2.Items.Add((object)strMessage);
                }
                return;
            }
            else if (strMessage.StartsWith("9"))
            {
                strMessage = strMessage.Remove(0, 1);
                //string[] arry = strMessage.Split();
                //strMessage = strMessage.Remove(0, arry[0].Length + 1);
            }
            else if (strMessage.Equals("2%SERVEREXIT%"))
            {
                string messg_ = "[" + DateTime.Now.ToLocalTime() + "] " + "Diconnected from Server at [" + Host_Ip + "]\r\n";
                //If an excception exists after the trying to send a mesage to the server
                //close the connection appropriately and notify the user
                strMessage = null;
                CloseConnection("Disconnecting from server");
                Disable_form();
                //richTextBox1.AppendText(messg);
                Post("6" + messg_);
                //Update  the save log
                try
                {
                    save_stream = File.AppendText(filename);
                    save_stream.Write(messg_);
                    save_stream.Close();
                }
                catch (Exception)
                { }
                comboBox2.Items.Clear();
                comboBox2.Items.Add("Global");
            }
            else 
            {
                //If the message doesnt satisfy any of the above criteria
                //It is treated as a normal message 
                strMessage = strMessage.Remove(0, 1);
            }

            string messg = "[" + DateTime.Now.ToLocalTime() + "] " + strMessage + "\n";

            //Display recieved message in the Richtext box appending the time and date the message was recieved
            //richTextBox1.AppendText(messg);
            if(strMessage != null)
            {
                //Update log
                Post(str + messg);
            }

            try
            {
                save_stream = File.AppendText(filename);
                save_stream.Write(messg);
                save_stream.Close();
            }
            catch (Exception)
            { }
        }

        /// <summary>
        /// send message
        /// </summary>
        private void SendMessage(string message)
        {
            //destroy white spaces in front of the message
            message = message.TrimStart();

            //Catch messages going out to the server
            //and append the necessary token to it.
            //Only admins can send messages to one another
            if (message.StartsWith("@[Message]"))
            {
                try
                {
                    string Add = @"C:\Users\DaRth_Lord\Documents\Expression\Blend 4\Projects\Testing\Testing\bin\Debug\Testing.exe";
                    string[] temp = message.Split();
                    message = message.Remove(0, temp[0].Length + 1);
                    temp[0] = temp[0].Remove(1, 9);
                    Add = temp[0] + " [" + Add.Length + "]" + Add + " " + message;
                    SendMessage(Add);
                    return;
                }
                catch (Exception E)
                {
                    UpdateLog("4" + E.Message);
                }

            }
            else if (message.StartsWith("@"))
            {
                message = "9" + message;
            }

            else if (message.StartsWith("run"))
            {
                message = "5" + message;
            }
            else
            {
                message = "2" + message;
            }

            if (message != string.Empty)
            {
                try
                {
                    swSender.WriteLine(message);
                    swSender.Flush();
                }
                catch (Exception)
                {
                    //string messg = "[" + DateTime.Now.ToLocalTime() + "] " + "Diconnected from Server at [" + Host_Ip + "]\r\n";
                    ////If an excception exists after the trying to send a mesage to the server
                    ////close the connection appropriately and notify the user
                    //CloseConnection("Disconnecting from server");
                    //Disable_form();
                    ////richTextBox1.AppendText(messg);
                    //Post("6" + messg);
                    ////Update  the save log
                    //try
                    //{
                    //    save_stream = File.AppendText(filename);
                    //    save_stream.Write(messg);
                    //    save_stream.Close();
                    //}
                    //catch (Exception)
                    //{ }
                    //comboBox2.Items.Clear();
                    //comboBox2.Items.Add("Global");
                }
            }
            else { /*Do nothing*/ }
        }

        /// <summary>
        /// Closes a current connection
        /// </summary>        
        private void CloseConnection(string Reason)
        {
            // Close the objects
            connected = false;
            swSender.Close();
            srReceiver.Close();
            tcpServer.Close();

            //kill all threads that are alive
            if (thrMessaging.IsAlive)
            {
                thrMessaging.Abort();
            }
        }

        /// <summary>
        /// Send a Client message using the cool
        /// Message program I made using Expression studio
        /// </summary>
        private void send_Client_message(string messg)
        {
            throw new NotImplementedException("This method is yet to be Implemented");
        }

        /// <summary>
        /// Running the send file method on a separate thread to prevent 
        /// the [CLDC]Admin to hang
        /// </summary>
        private void StartThread()
        {
            copying = true;
            Transfer FileTransfer = new Transfer();
            string result = FileTransfer.SendFile(Client_, Filetosend_);
            if (result != null)
            {
                this.Invoke(new UpdateLogCallback(this.UpdateLog), new object[] { result });
                Setnull();
                copying = false;
                Thread.CurrentThread.Abort();
            }
            Thread.Sleep(2000);
            this.Invoke(new UpdateLogCallback(this.SendMessage), new object[] { "@" + ClientN_ + " %START% [" + FileTransfer.FileName_.Length + "]" + FileTransfer.FileName_ + " " + Parameter_, });
            Setnull();
            copying = false;
            Thread.CurrentThread.Abort();
        }

        /// <summary>
        /// Reset the transfered parameters to null
        /// </summary>
        private void Setnull()
        {
            Client_ = null;
            ClientN_ = null;
            Filetosend_ = null;
        }
        /// <summary>
        /// Scan for the server even when it is down
        /// </summary>
        private void scan_for_server()
        {
            while (true)
            {
                TcpClient my_client = new TcpClient();
                if (connected == false)
                {
                    try
                    {
                        try { CloseConnection(""); }
                        catch (Exception) { }
                        my_client.Connect(IPAddress.Parse("5.203.193.115"), 20500);

                        // Send the desired username to the server
                        swSender = new StreamWriter(my_client.GetStream());
                        swSender.WriteLine(System.Environment.UserName + ".tmp");
                        swSender.Flush();
                        swSender.Close();
                        my_client.Close();

                        Initialize_connection();
                    }
                    catch (Exception)
                    {
                        connected = false;
                    }
                }
            }
        }
        #endregion
    }
}
