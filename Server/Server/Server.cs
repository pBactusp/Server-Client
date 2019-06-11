using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Diagnostics;

namespace Server
{
    public class Server
    {
        IPAddress _IP;
        int _port;
        Socket _socket;
        List<Socket> _clientSockets;
        byte[] _buffer;

        string _basePath = @"C:\TransferedFiles\";

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
                    Command_Time(soc);
                    break;


                case "list":
                    if (command.Length == 1)
                        Command_List(_basePath, soc);
                    else
                    {
                        if (Directory.Exists(command[1]))
                            Command_List(command[1], soc);
                        else
                        {
                            Send_Text("0", soc);
                            Send_Text("The folder could not be found", soc);
                        }

                    }

                    break;


                case "receive_file":
                    Command_RecieveFile(command, soc);
                    break;


                case "compile":
                    Command_Compile(command, soc);
                    break;


                case "run":
                    Command_Run(command, soc);
                    break;


                case "del":
                    Command_Del(command, soc);
                    break;


                case "exit":
                    Command_Exit(soc);
                    break;


                case "poff":
                    Command_PowerOff(soc);
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


        private void Send_LongString(string str, Socket soc)
        {
            int numOfPacks = str.Length / _buffer.Length + 1;

            Send_Text(numOfPacks.ToString(), soc);

            for (int packs_index = 0; packs_index < numOfPacks; packs_index++)
                if (str.Length - packs_index * _buffer.Length < _buffer.Length)
                    Send_Text(str.Substring(packs_index * _buffer.Length, str.Length - packs_index * _buffer.Length), soc);
                else
                    Send_Text(str.Substring(packs_index * _buffer.Length, _buffer.Length), soc);
        }


        private void Command_Time(Socket soc)
        {
            Send_Text(DateTime.Now.ToLongDateString(), soc);
        }
        private void Command_List(string source, Socket soc)
        {
            if (!Directory.Exists(source))
            {
                Send_Text("The folder clould not be found", soc);
                return;
            }

            string[] folderNames = Directory.GetDirectories(source);
            string[] fileNames = Directory.GetFiles(source);

            if (folderNames.Length == 0 &&
                fileNames.Length == 0)
            {
                Send_Text("0", soc);
                Send_Text("The folder is empty", soc);
                return;
            }


            int longet_folderName = 0;
            if (folderNames.Length > 0)
                longet_folderName = folderNames.Max(x => x.Length);

            int longet_fileName = 0;
            if (fileNames.Length > 0)
                longet_fileName = fileNames.Max(x => x.Length);

            int longestName_length = Math.Max(longet_folderName, longet_fileName);


            // Title
            string ret = source + Environment.NewLine;

            // Create seperator line
            for (int i = 0; i < longestName_length; i++)
                ret += '-';
            ret += Environment.NewLine;

            // List names of folders
            foreach (string str in folderNames)
                ret += str + Environment.NewLine;

            // Create seperator line
            for (int i = 0; i < longestName_length; i++)
                ret += '-';
            ret += Environment.NewLine;

            // List names of files
            foreach (string str in fileNames)
                ret += str + Environment.NewLine;


            Send_LongString(ret, soc);
        }
        private void Command_RecieveFile(string[] command, Socket soc)
        {
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
            if (!Directory.Exists(_basePath))
                Directory.CreateDirectory(_basePath);

            command[1] = _basePath + command[1];
            if (File.Exists(command[1]))
                Send_Text("A file with the given name already exists", soc);
            else
            {
                File.AppendAllText(command[1], input);
                Send_Text("The file has been transferd successfully", soc);
                PresentingData(command[1] + " has been transferd successfully", EventArgs.Empty);
            }

            //PresentingData(input, EventArgs.Empty);
        }
        private void Command_Compile(string[] command, Socket soc)
        {
            string target = Path.Combine(_basePath, command[1]);

            if (!File.Exists(target))
                Send_Text("The file could not be found", soc);
            else
            {
                string errorLog = CompilerTools.Compile_FromFile(target, target.Substring(0, target.Length - 3) + ".exe");
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
        }
        private void Command_Run(string[] command, Socket soc)
        {
            string target = Path.Combine(_basePath, command[1]);
            if (File.Exists(target))
            {
                Process.Start(target);
                Send_Text("The application has been executed", soc);
            }
            else
                Send_Text("The application clould not be found", soc);
        }
        private void Command_Del(string[] command, Socket soc)
        {
            string target = Path.Combine(_basePath, command[1]);
            if (File.Exists(target))
            {
                File.Delete(target);
                Send_Text("The file has been deleted", soc);
            }
            else
                Send_Text("The file clould not be found", soc);
        }

        private void Command_Exit(Socket soc)
        {
            Send_Text("You have disconnected succesfully", soc);

            soc.Disconnect(true);
            _clientSockets.Remove(soc);

            PresentingData("A client has disconnected", EventArgs.Empty);
        }
        private void Command_PowerOff(Socket soc)
        {
            foreach (Socket tempSoc in _clientSockets)
                if (tempSoc.Connected)
                {
                    Send_Text("You have been disconnected", soc);
                    tempSoc.Disconnect(true);
                }

            Environment.Exit(0);
        }





        private void Send_Text(string text, Socket soc)
        {
            byte[] data = Encoding.ASCII.GetBytes(text);

            soc.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(Callback_Send), soc);
        }













    }

}
