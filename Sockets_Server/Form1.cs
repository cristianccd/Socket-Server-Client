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
using System.Threading;
using System.Net;
using System.Timers;

namespace Sockets_Server
{
    public partial class Form1 : Form
    {
        //VARS
        int Status = 0;
        TcpListener serverSocket = new TcpListener(8888);
        int requestCount = 0;
        TcpClient clientSocket = default(TcpClient);
        NetworkStream serverStream;
        byte[] inStream = new byte[4096];
        int bytesRead;
        string Returned_Data;
        string Server_Response;
        Byte[] sendBytes;
        int MsgCount = 0;
        static private System.Timers.Timer aTimer, bTimer, cTimer;
        int lastAlive = 0;

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //Try the connection
            try
            {
                serverSocket.Start();
                listBox1.Items.Add(">> Server Started");
                aTimer = new System.Timers.Timer(100);
                // Hook up the Elapsed event for the timer. 
                aTimer.Elapsed += new ElapsedEventHandler(OnTimedEventA);
                aTimer.Enabled = true;
            }
            catch (Exception h)
            {
                MessageBox.Show(h.Message, "Error starting server!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            clientSocket.Close();
            serverSocket.Stop();
        }


        private void OnTimedEventA(Object source, ElapsedEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(delegate
                {
                    if (!serverSocket.Pending())
                    {
                        aTimer.Enabled = true;
                        return;
                    }
                    try
                    {
                        clientSocket = serverSocket.AcceptTcpClient();
                        listBox1.Items.Add(">> Accepted connection from client");
                    }
                    catch (Exception h)
                    {
                        MessageBox.Show(h.Message, "Error starting server!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    //Set initial status
                    Status = 0;
                    MsgCount = 0;
                    lastAlive = 0;
                    serverStream = clientSocket.GetStream();
                    serverStream.ReadTimeout = 2000;
                    //Enable timer
                    bTimer = new System.Timers.Timer(100);
                    // Hook up the Elapsed event for the timer. 
                    bTimer.Elapsed += new ElapsedEventHandler(OnTimedEventB);
                    bTimer.Enabled = true;
                }));
            }
        }

        private void OnTimedEventB(Object source, ElapsedEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(delegate
                {

                    switch (Status)
                    {
                        case 0:
                            //Initial state. Send a handshake
                            try
                            {
                                Server_Response = "<ServerACK></ServerACK>";
                                sendBytes = Encoding.ASCII.GetBytes(Server_Response);
                                serverStream.Write(sendBytes, 0, sendBytes.Length);
                                serverStream.Flush();
                                Status = 1;
                            }
                            catch (Exception h)
                            {
                                MessageBox.Show(h.Message, "Error sending server handshake!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }
                            break;
                        case 1:
                            // Receive a handshake
                            try
                            {
                                bytesRead = serverStream.Read(inStream, 0, inStream.Length);
                                Returned_Data = System.Text.Encoding.ASCII.GetString(inStream, 0, bytesRead);
                                for (int i = 0; i < 5; i++)
                                {
                                    //If ACK go to next status
                                    if (Returned_Data == "<ClientACK></ClientACK>")
                                    {
                                        Status = 2;
                                        listBox1.Items.Add(">> Client ACK received!");
                                        cTimer = new System.Timers.Timer(1000);
                                        // Hook up the Elapsed event for the timer. 
                                        cTimer.Elapsed += new ElapsedEventHandler(OnTimedEventC);
                                        cTimer.Enabled = true;
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
                                MessageBox.Show(h.Message, "Error receiving client handshake!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }
                            break;
                        case 2:
                            if (MsgCount < 100)
                            {
                                //Send 10 messages
                                MsgCount++;
                                try
                                {
                                    Server_Response = "Message " + MsgCount + " - " + DateTime.Now;
                                    sendBytes = Encoding.ASCII.GetBytes(Server_Response);
                                    serverStream.Write(sendBytes, 0, sendBytes.Length);
                                    serverStream.Flush();
                                }
                                catch (Exception h)
                                {
                                    MessageBox.Show(h.Message, "Error sending data!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    return;
                                }
                            }
                            else
                            {
                                Server_Response = "<EOF>";
                                sendBytes = Encoding.ASCII.GetBytes(Server_Response);
                                serverStream.Write(sendBytes, 0, sendBytes.Length);
                                serverStream.Flush();
                                bTimer.Enabled = false;
                                cTimer.Enabled = false;
                                return;
                            }
                            break;
                        case 3:
                            break;
                    }
                }));
            }
        }


        private void OnTimedEventC(Object source, ElapsedEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(delegate
                {
                    bytesRead = serverStream.Read(inStream, 0, inStream.Length);
                    Returned_Data = System.Text.Encoding.ASCII.GetString(inStream, 0, bytesRead);
                    if (Returned_Data == "<Alive>")
                    {
                        listBox1.Items.Add(">> Alive");
                        lastAlive = 0;
                    }
                    else
                        lastAlive += 100;
                    if (lastAlive > 1000)
                    {
                        listBox1.Items.Add(">> Timeout");
                        cTimer.Enabled = false;
                        bTimer.Enabled = false;
                        return;
                    }
                }));
            }
        }
    }
}
