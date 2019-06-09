using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Client
{
    class Program
    {
        static Client _client;


        static void Main(string[] args)
        {
            Console.Title = "Client";

            _client = new Client(100);
            _client.PresentingData += LogClientInput;
            _client.Init();

            SendLoop(_client);
        }


        static string[] Split(string str)
        {
            bool in_quotations = false;
            List<int> splits = new List<int>();

            for (int i = 0; i < str.Length; i++)
                switch (str[i])
                {
                    case ' ':
                        if (!in_quotations)
                            splits.Add(i);
                        break;

                    case '"':
                        in_quotations = !in_quotations;
                        break;
                }

            string[] ret;
            if (splits.Count > 0)
            {
                splits.Add(str.Length);
                ret = new string[splits.Count];
                ret[0] = str.Substring(0, splits[0]);
                for (int i = 1; i < splits.Count; i++)
                {
                    ret[i] = str.Substring(splits[i - 1], splits[i] - splits[i - 1]);
                }
            }
            else
                ret = new string[1] { str };

            // Clear quotation marks
            for (int i = 0; i < ret.Length; i++)
                if (ret[i][0] == '"')
                    ret[i] = ret[i].Substring(1, ret[i].Length - 2);
                else if (ret[i][1] == '"')
                    ret[i] = ret[i].Substring(2, ret[i].Length - 3);

            return ret;
        }

        static void SendLoop(Client cli)
        {
            string input;

            while (true)
            {
                Console.WriteLine("Type a command:");
                input = Console.ReadLine();

                if (input != "")
                {
                    string[] command = Split(input);

                    switch (command[0].ToLower())
                    {
                        case "time":
                        case "gettime":
                        case "get_time":
                            cli.Send_Text("get_time");
                            cli.Recieve_Text();
                            break;


                        case "sendfile":
                        case "send_file":
                            _client.Send_File(command[1], command[2]);
                            break;


                        case "compile":
                            cli.Send_Text("compile" + command[1]);
                            break;


                        case "exit":
                            cli.Send_Text("exit");
                            cli.Disconnect();
                            Environment.Exit(0);
                            break;


                        case "poff":
                        case "poweroff":
                        case "power_off":
                            cli.Send_Text("poff");
                            cli.Recieve_Text();
                            Environment.Exit(0);
                            break;

                    }

                }


            }



        }



        static void LogClientInput(object sender, EventArgs e)
        {
            if ((string)sender == "")
                Console.WriteLine();
            else
                Console.WriteLine('[' + DateTime.Now.ToString("hh:mm:ss") + "] " + (string)sender);
        }


    }
}
