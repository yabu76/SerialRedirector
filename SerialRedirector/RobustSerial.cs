using System;
using System.IO;
using System.IO.Ports;
using System.Collections.Generic;

namespace SerialRedirector
{
    internal class RobustSerial
    {
        public enum Parity { None = 0, Odd, Even, Mark, Space, };
        public enum StopBits { None = 0, One, OnePointFive, Two, };

        private Stream theBaseStream;

        private SerialPort _sp;
        private string _portName;
        private int _baudRate;
        private Parity _parity;
        private int _dataBits;
        private StopBits _stopBits;
        private bool _isAttached = false;

        private static readonly Dictionary<Parity, System.IO.Ports.Parity> _dictParity =
            new Dictionary<Parity, System.IO.Ports.Parity>
        {
            { Parity.None, System.IO.Ports.Parity.None },
            { Parity.Odd, System.IO.Ports.Parity.Odd },
            { Parity.Even, System.IO.Ports.Parity.Even },
            { Parity.Mark, System.IO.Ports.Parity.Mark },
            { Parity.Space, System.IO.Ports.Parity.Space },
        };

        private static readonly Dictionary<StopBits, System.IO.Ports.StopBits> _dictStopBits =
            new Dictionary<StopBits, System.IO.Ports.StopBits>
        {
            { StopBits.None, System.IO.Ports.StopBits.None },
            { StopBits.One, System.IO.Ports.StopBits.One },
            { StopBits.OnePointFive, System.IO.Ports.StopBits.OnePointFive },
            { StopBits.Two, System.IO.Ports.StopBits.Two },
        };

        public int BytesToRead
        {
            get
            {
                return _sp.BytesToRead;
            }
        }

        public bool DtrEnable
        {
            get
            {
                return _sp.DtrEnable;
            }
            set
            {
                _sp.DtrEnable = value;
            }
        }

        public bool RtsEnable
        {
            get
            {
                return _sp.RtsEnable;
            }
            set
            {
                _sp.RtsEnable = value;
            }
        }

        public RobustSerial(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            _portName = portName;
            _baudRate = baudRate;
            _parity = parity;
            _dataBits = dataBits;
            _stopBits = stopBits;
            _sp = new SerialPort(portName, baudRate, toIPParity(parity), dataBits, toIPStopBits(stopBits));
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public void Dispose(bool disposing)
        {
            if (disposing && (_sp.Container != null))
            {
                _sp.Container.Dispose();
            }
            try
            {
                if (theBaseStream.CanRead)
                {
                    theBaseStream.Close();
                    GC.ReRegisterForFinalize(theBaseStream);
                }
            }
            catch
            {
                // ignore exception - bug with USB - serial adapters.
            }
            _sp.Dispose();
        }

        public void Open()
        {
            _sp.Open();
            theBaseStream = _sp.BaseStream;
            GC.SuppressFinalize(_sp.BaseStream);
            _isAttached = true;
        }

        public void Close()
        {
            _sp.Close();
            _isAttached = false;
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            return _sp.Read(buffer, offset, count);
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            _sp.Write(buffer, offset, count);
        }

        public void FoundDisconnect()
        {
            Close();
        }

        public void FoundConnect()
        {
            Open();
        }

        public bool IsAttached()
        {
            return _isAttached;
        }

        private System.IO.Ports.Parity toIPParity(Parity parity)
        {
            return _dictParity[parity];
        }

        private System.IO.Ports.StopBits toIPStopBits(StopBits stopBits)
        {
            return _dictStopBits[stopBits];
        }
    }
}
