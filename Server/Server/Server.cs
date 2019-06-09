using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace Server
{
    public class Server
    {
        IPAddress _IP;
        int _port;
        Socket _socket;

        List<Socket> _clientSockets;

        byte[] _buffer;



        public EventHandler PresentingData;



        public Server(int port, int maxConnections, int bufferSize = 1024)
        {
            _port = port;
            _clientSockets = new List<Socket>(maxConnections);

            _buffer = new byte[bufferSize];
        }

        public void Init()
        {
            PresentingData("Initializing server...", EventArgs.Empty);

            _IP = IPAddress.Any;
            PresentingData("IP: " + _IP.ToString(), EventArgs.Empty);

            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.Bind(new IPEndPoint(_IP, _port));
            _socket.Listen(_clientSockets.Capacity);
            _socket.BeginAccept(new AsyncCallback(Callback_Accept), null);

            PresentingData("The server has initialized succesfully.", EventArgs.Empty);



        }


        private void Callback_Accept(IAsyncResult AR)
        {
            Socket soc = _socket.EndAccept(AR);
            _clientSockets.Add(soc);
            PresentingData("A client has connected", EventArgs.Empty);
            soc.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(Callback_Recieve), soc);
            _socket.BeginAccept(new AsyncCallback(Callback_Accept), soc);

        }

        private void Callback_Recieve(IAsyncResult AR)
        {
            Socket soc = (Socket)AR.AsyncState;
            int recieved = soc.EndReceive(AR);
            byte[] dataBuffer = new byte[recieved];
            Array.Copy(_buffer, dataBuffer, recieved);

            string inputText = Encoding.ASCII.GetString(dataBuffer);

            PresentingData("Text recieved: " + inputText, EventArgs.Empty);

            string[] command = inputText.Split(' ');

            switch (command[0].ToLower())
            {
                case "time":
                case "get_time":
                case "gettime":
                    Send_Text(DateTime.Now.ToLongDateString(), soc);
                    break;


                case "send_file":
                    // Read file
                    byte[] recBuffer = new byte[_buffer.Length];
                    int rec;
                    byte[] data;

                    string input = "";
                    for (int i = 0; i < int.Parse(command[2]); i++)
                    {
                        rec = soc.Receive(recBuffer);
                        data = new byte[rec];
                        Array.Copy(recBuffer, data, rec);
                        input += Encoding.ASCII.GetString(data);
                    }

                    // Create file
                    if (!Directory.Exists(@"C:\TransferedFiles"))
                        Directory.CreateDirectory(@"C:\TransferedFiles");

                    command[1] = @"C:\TransferedFiles\" + command[1];
                    if (File.Exists(command[1]))
                        Send_Text("A file with the given name already exists", soc);
                    else
                    {
                        File.AppendAllText(command[1], input);
                        Send_Text("The file has been transferd successfully", soc);
                        PresentingData(command[1] + " has been transferd successfully", EventArgs.Empty);
                    }

                    //PresentingData(input, EventArgs.Empty);
                    break;


                case "compile":
                    if (!File.Exists(command[1]))
                        Send_Text("The file could not be found", soc);
                    else
                    {
                        string errorLog = CompilerTools.Compile_FromFile(command[1], command[1].Substring(0, command[1].Length - 3) + ".exe");
                        if (errorLog == "")
                        {
                            Send_Text("The file has been compiled", soc);
                            PresentingData("The file has been compiled", EventArgs.Empty);
                        }
                        else
                        {
                            Send_Text("Failed to compile:" + Environment.NewLine, soc);
                            PresentingData("The file failed to compile", EventArgs.Empty);
                        }
                    }
                    break;


                case "exit":
                    Send_Text("You have disconnected succesfully", soc);

                    soc.Disconnect(true);
                    _clientSockets.Remove(soc);

                    PresentingData("A client has disconnected", EventArgs.Empty);
                    break;


                case "poff":
                case "poweroff":
                case "power_off":
                    foreach (Socket tempSoc in _clientSockets)
                    {
                        if (tempSoc.Connected)
                        {
                            Send_Text("You have been disconnected", soc);
                            tempSoc.Disconnect(true);
                        }
                    }

                    Environment.Exit(0);
                    break;

                default:
                    string tempError = "The command: '" + command[0] + "' was not recognized";
                    PresentingData(tempError, EventArgs.Empty);
                    Send_Text(tempError, soc);
                    break;
            }

            if (command[0] != "exit")
                soc.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(Callback_Recieve), soc);
        }

        private void Callback_Send(IAsyncResult AR)
        {
            Socket soc = (Socket)AR.AsyncState;
            soc.EndSend(AR);
        }


        private void Send_Text(string text, Socket soc)
        {
            byte[] data = Encoding.ASCII.GetBytes(text);

            soc.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(Callback_Send), soc);
        }













    }

}
