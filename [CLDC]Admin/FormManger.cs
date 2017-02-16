using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace _CLDC_Admin
{
    public partial class Form1
    {
        private string PreviousMessage = "";
        public void Post(string Message)
        {
            if (Message.Contains("Disconnected from Server at [") && PreviousMessage.Contains("Disconnected from Server at ["))
            {
                return;
            }
            PreviousMessage = Message;
            if(Message.StartsWith("2")||Message.StartsWith("6"))
            {
                Message = Message.Remove(0, 1);
                richTextBox1.AppendText(Message);
                richTextBox5.AppendText(Message);
            }
            else if (Message.StartsWith("4"))
            {
                Message = Message.Remove(0,1);
                richTextBox1.AppendText(Message);
                richTextBox4.AppendText(Message);
            }
            else if (Message.StartsWith("5")||Message.StartsWith("9"))
            {
                Message = Message.Remove(0, 1);
                richTextBox1.AppendText(Message);
                richTextBox6.AppendText(Message);
            }
            else if (Message.StartsWith("7") || Message.StartsWith("8"))
            {
                Message = Message.Remove(0, 1);
                richTextBox1.AppendText(Message);
                richTextBox2.AppendText(Message);
            }
            else
            {
                Message = Message.Remove(0, 1);
                richTextBox1.AppendText(Message);
            }
        }
    }
}
