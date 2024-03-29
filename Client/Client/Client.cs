﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;


namespace Client
{
    public class Client
    {
        Socket _socket;

        int _port;
        int _bufferSize;

        public EventHandler PresentingData;


        public Client(int port, int bufferSize = 1024)
        {
            _port = port;
            _bufferSize = bufferSize;
        }

        public void Init()
        {
            PresentingData("Initializing client...", EventArgs.Empty);

            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            LoopConnect(10);

        }

        private void LoopConnect(int maxAttempts)
        {
            int attempts = 0;

            while (!_socket.Connected && attempts < maxAttempts)
            {
                try
                {
                    attempts++;
                    _socket.Connect(IPAddress.Loopback, _port);
                }
                catch (SocketException)
                {
                    PresentingData("Failed connection attempts: " + attempts + " has failed", EventArgs.Empty);
                }
            }


            PresentingData("", EventArgs.Empty);
            if (_socket.Connected)
                PresentingData("Connected", EventArgs.Empty);
            else
                PresentingData("Failed to connect", EventArgs.Empty);
        }


        string ReadFile(string source)
        {
            string ret = "";
            using (StreamReader sr = new StreamReader(source))
            {
                while (!sr.EndOfStream)
                {
                    ret += sr.ReadLine();
                    ret += Environment.NewLine;
                }
                sr.Close();
            }

            return ret;
        }


        public string Recieve_Text(bool echo = true)
        {
            byte[] recBuffer = new byte[_bufferSize];
            int rec = _socket.Receive(recBuffer);

            byte[] data = new byte[rec];
            Array.Copy(recBuffer, data, rec);

            if (echo)
                PresentingData(Encoding.ASCII.GetString(data), EventArgs.Empty);

            return Encoding.ASCII.GetString(data);
        }
        public void Send_Text(string text)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(text);
            _socket.Send(buffer);
        }
        public void Send_File(string source, string targetName)
        {
            string output = ReadFile(source);
            int numOfPacks = output.Length / _bufferSize + 1;
            Send_Text("receive_file" + targetName + " " + numOfPacks);

            for (int packs_index = 0; packs_index < numOfPacks; packs_index++)
                if (output.Length - packs_index * _bufferSize < _bufferSize)
                    Send_Text(output.Substring(packs_index * _bufferSize, output.Length - packs_index * _bufferSize));
                else
                    Send_Text(output.Substring(packs_index * _bufferSize, _bufferSize));

            Recieve_Text();
        }

        public string Recieve_File(int numOfPacks, bool echo = true)
        {
            byte[] recBuffer = new byte[_bufferSize];
            int rec;
            byte[] data;

            string input = "";
            for (int i = 0; i < numOfPacks; i++)
            {
                rec = _socket.Receive(recBuffer);
                data = new byte[rec];
                Array.Copy(recBuffer, data, rec);
                input += Encoding.ASCII.GetString(data);
            }

            if (echo)
                PresentingData(input, EventArgs.Empty);

            return input;
        }

        public void Disconnect()
        {
            _socket.Disconnect(true);
        }


    }

}
