﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.IO;
using System.Threading;

namespace SerialRedirector
{
    class Program
    {
        static SerialPort sp1 = null;
        static SerialPort sp2 = null;
        private const string SP1_PORT = "COM11";
        private const string SP2_PORT = "COM12";
        private const int BAUD_RATE = 115200;
        private const Parity PARITY = Parity.None;
        private const int DATA_BITS = 8;
        private const StopBits STOP_BITS = StopBits.One;
        private const int BUF_SIZE = 8192;
        private const int BRIDGE_INTERVAL_DURING_FREE_TIME = 5; /* [ms] */

        private static int Min_(int a, int b)
        {
            return (a < b) ? a : b;
        }

        private static void ParseOpts(string[] args)
        {

        }

        private static void BridgePorts(SerialPort sp1, SerialPort sp2)
        {
            int avails;
            bool hasTraffic;
            byte[] buf = new byte[BUF_SIZE];

            for (;;)
            {
                hasTraffic = false;
                avails = Min_(sp1.BytesToRead, BUF_SIZE);
                if (avails > 0)
                {
                    hasTraffic = true;
                    sp1.Read(buf, 0, avails);
                    sp2.Write(buf, 0, avails);
                }
                avails = Min_(sp2.BytesToRead, BUF_SIZE);
                if (avails > 0)
                {
                    hasTraffic = true;
                    sp2.Read(buf, 0, avails);
                    sp1.Write(buf, 0, avails);
                }
                if ( ! hasTraffic)
                {
                    Thread.Sleep(BRIDGE_INTERVAL_DURING_FREE_TIME); /* [ms] */
                }
            }
        }

        static void Main(string[] args)
        {
            Console.WriteLine("SerialRedirector Start.");

            sp1 = new SerialPort(SP1_PORT, BAUD_RATE, PARITY, DATA_BITS);
            sp2 = new SerialPort(SP2_PORT, BAUD_RATE, PARITY, DATA_BITS);
            sp1.Open();
            sp2.Open();
            sp1.DtrEnable = true;
            sp1.RtsEnable = true;
            sp2.DtrEnable = true;
            sp2.RtsEnable = true;

            BridgePorts(sp1, sp2);

            sp2.RtsEnable = false;
            sp2.DtrEnable = false;
            sp1.RtsEnable = false;
            sp1.DtrEnable = false;
            sp2.Close();
            sp1.Close();
            sp2.Dispose();
            sp2.Dispose();

            Console.WriteLine("SerialRedirector Stop.");
        }
    }
}
