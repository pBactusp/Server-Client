using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Server
{
    class Program
    {
        static Server _server;

        static void Main(string[] args)
        {
            Console.Title = "Server";

            _server = new Server(100, 5);
            _server.PresentingData += LogServerInput;
            _server.Init();



            Console.ReadLine();
        }


        static void LogServerInput(object sender, EventArgs e)
        {
            if ((string)sender == "")
                Console.WriteLine();
            else
                Console.WriteLine('[' + DateTime.Now.ToString("hh:mm:ss") + "] " + (string)sender);

        }


    }

}
