using System;
using System.Threading;
using LibUsbDotNet.DeviceNotify;

namespace SerialRedirector
{
    class Program
    {
        private static string _sp1Name;
        private static string _sp2Name;
        private static int _baudRate;
        private const RobustSerial.Parity PARITY = RobustSerial.Parity.None;
        private const int DATA_BITS = 8;
        private const RobustSerial.StopBits STOP_BITS = RobustSerial.StopBits.One;

        static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.Write("usage: SerialRedirector COM<i> COM<j> <baudrate>\n");
                return;
            }
            _sp1Name = args[0];
            _sp2Name = args[1];
            _baudRate = int.Parse(args[2]);

            Thread.GetDomain().UnhandledException +=
                new UnhandledExceptionEventHandler(UnhandledException);


            SerialRedirector redirector = new SerialRedirector(_sp1Name, _sp2Name, _baudRate, PARITY, DATA_BITS, STOP_BITS);

            Console.WriteLine("Redirect Start. ({0}<=>{1}, {2}bps, Parity{3}, {4}BitsChar, {5}StopBits)\n",
                _sp1Name, _sp2Name, _baudRate, PARITY, DATA_BITS, STOP_BITS);

            redirector.DoRedirect();

            Console.WriteLine("Redirect Stop.");
        }

        public static void UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exObj = e.ExceptionObject;
            if (exObj != null)
            {
                var exType = e.ExceptionObject.GetType();
                if (exType == typeof(ObjectDisposedException))
                {
                    // .NET3.5のSerialPortのバグで、ケーブル挿抜後にハンドル不能の
                    // ObjectDisposedExceptionが返ることがある。このため、未ハンドル例外処理にて
                    // ObjectDisposedExceptionについてのみ無視する。

                }
            }
            else
            {
                Environment.Exit(0);
            }
        }
    }
}
