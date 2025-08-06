using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Timers;

namespace Sockets_Client
{
    public partial class Form1 : Form
    {
        //VARS
        System.Net.Sockets.TcpClient clientSocket = new System.Net.Sockets.TcpClient();
        int Status = 0; //Status of the machine
        int Count = 0; //Message counter
        byte[] inStream = new byte[4096]; //Received message
        NetworkStream serverStream; //Streamer
        int bytesRead; //Received bytes read
        string Returned_Data; //Returned data
        Byte[] sendBytes;
        string Client_Response;
        static private System.Timers.Timer aTimer, bTimer;
        double Accum = 0;

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //Try the connection
            try
            {
                clientSocket.Connect("127.0.0.1", 8888);
            }
            catch (Exception h)
            {
                MessageBox.Show(h.Message, "Error starting client!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            listBox1.Items.Add(">> Client Started...");
            listBox1.Items.Add(">> Connected to Server...");
            //Set initial status
            Status = 0;
            Accum = 0;
            serverStream = clientSocket.GetStream();
            serverStream.ReadTimeout = 2000;
            //Enable timer

            //timer1.Enabled = true;
            aTimer = new System.Timers.Timer(100);
            // Hook up the Elapsed event for the timer. 
            aTimer.Elapsed += new ElapsedEventHandler(OnTimedEventA);
            bTimer = new System.Timers.Timer(1000);
            // Hook up the Elapsed event for the timer. 
            bTimer.Elapsed += new ElapsedEventHandler(OnTimedEventB);
            aTimer.Enabled = true;
            bTimer.Enabled = true;
        }

        private void OnTimedEventA(Object source, ElapsedEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(delegate
                {
                    switch (Status)
                    {
                        case 0:
                            //Initial state. Receive a handshake
                            try
                            {
                                bytesRead = serverStream.Read(inStream, 0, inStream.Length);
                                Returned_Data = System.Text.Encoding.ASCII.GetString(inStream, 0, bytesRead);
                                for (int i = 0; i < 5; i++)
                                {
                                    //If ACK go to next status
                                    if (Returned_Data == "<ServerACK></ServerACK>")
                                    {
                                        Status = 1;
                                        listBox1.Items.Add(">> Server ACK received!");
                                        break;
                                    }
                                    else
                                    {
                                        listBox1.Items.Add("Retry: " + i);
                                        System.Threading.Thread.Sleep(1000);
                                        listBox1.Items.Add("Connection Timed Out...");
                                        //Close the connection if opened
                                        if (clientSocket.Connected)
                                            clientSocket.Close();
                                        return;
                                    }
                                }
                            }
                            catch (Exception h)
                            {
                                MessageBox.Show(h.Message, "Error receiving server handshake!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }
                            break;
                        case 1:
                            //Send a handshake
                            try
                            {
                                Client_Response = "<ClientACK></ClientACK>";
                                sendBytes = Encoding.ASCII.GetBytes(Client_Response);
                                serverStream.Write(sendBytes, 0, sendBytes.Length);
                                serverStream.Flush();
                                bTimer.Enabled = true;
                                Status = 2;
                            }
                            catch (Exception h)
                            {
                                MessageBox.Show(h.Message, "Error sending client handshake!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }
                            break;
                        case 2:
                            try
                            {
                                bytesRead = serverStream.Read(inStream, 0, inStream.Length);
                                Accum += bytesRead;
                                Returned_Data = System.Text.Encoding.ASCII.GetString(inStream, 0, bytesRead);
                                listBox1.Items.Add("RX: " + Accum + "- " + Returned_Data);
                                if (Returned_Data == "<EOF>")
                                {
                                    aTimer.Enabled = false;
                                    Status = 0;
                                    return;
                                }                                    
                            }
                            catch (Exception h)
                            {
                                MessageBox.Show(h.Message, "Error receiving server handshake!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }
                            break;
                        case 3:
                            break;
                    }
                }));
            }
        }

        private void OnTimedEventB(Object source, ElapsedEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(delegate
                {
                    try
                            {
                                Client_Response = "<Alive>";
                                sendBytes = Encoding.ASCII.GetBytes(Client_Response);
                                serverStream.Write(sendBytes, 0, sendBytes.Length);
                                serverStream.Flush();
                            }
                    catch (Exception h)
                    {
                        MessageBox.Show(h.Message, "Error sending client handshake!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }));
            }
        }
        private void button2_Click(object sender, EventArgs e)
        {
            //Close the connection if opened
            if (clientSocket.Connected)
                clientSocket.Close();
        }
    }
}
