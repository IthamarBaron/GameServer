using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace GameServer
{
    internal class Program
    {
        public static bool isRunning = false;

        static void Main(string[] args)
        {
            Console.Title = "Game server 0.1";

            isRunning = true;

            Thread mainThread = new Thread(new ThreadStart(MainThread));
            mainThread.Start();

            Server.Start(15, 26950);

        }

        public static void MainThread()
        {
            Console.WriteLine("Main thread started. Running at " + Constants.TICKS_PER_SEC + " ticks per second.");
            //when the next server tick should be executed
            DateTime _nextLoop = DateTime.Now;

            while (isRunning)
            {
                while (_nextLoop < DateTime.Now)
                {
                    GameLogic.Update();

                    //updateing when next tick should happend
                    _nextLoop = _nextLoop.AddMilliseconds(Constants.MS_PER_TICK);

                    //to avoid server using a lot of CPU power 
                    if (_nextLoop > DateTime.Now) // if the server is in the "future"
                    {
                        Thread.Sleep(_nextLoop - DateTime.Now);//we make it wait
                    }
                }
            }

        }
    }
}
