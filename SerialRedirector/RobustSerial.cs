using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;

namespace SerialRedirector
{
    class RobustSerial
    {
        public enum Parity { None = 0, Odd, Even, Mark, Space, };
        public enum StopBits { None = 0, One, OnePointFive, Two, };

        private SerialPort sp_;
        private string portName_;
        private int baudRate_;
        private Parity parity_;
        private int dataBits_;
        private StopBits stopBits_;

        private static readonly Dictionary<Parity, System.IO.Ports.Parity> dictParity_ = 
            new Dictionary<Parity, System.IO.Ports.Parity>
        {
            { Parity.None, System.IO.Ports.Parity.None },
            { Parity.Odd, System.IO.Ports.Parity.Odd },
            { Parity.Even, System.IO.Ports.Parity.Even },
            { Parity.Mark, System.IO.Ports.Parity.Mark },
            { Parity.Space, System.IO.Ports.Parity.Space },
        };

        private static readonly Dictionary<StopBits, System.IO.Ports.StopBits> dictStopBits_ =
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
                int ret;
                ret = sp_.BytesToRead;
                return ret;
            }
        }

        public bool DtrEnable
        {
            get
            {
                bool ret;
                ret = sp_.DtrEnable;
                return ret;
            }
            set
            {
                sp_.DtrEnable = value;
            }
        }

        public bool RtsEnable
        {
            get
            {
                bool ret;
                ret = sp_.RtsEnable;
                return ret;
            }
            set
            {
                sp_.RtsEnable = value;
            }
        }

        public RobustSerial(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            portName_ = portName;
            baudRate_ = baudRate;
            parity_ = parity;
            dataBits_ = dataBits;
            stopBits_ = stopBits;
            try
            {
                sp_ = new SerialPort(portName, baudRate, toIPParity(parity), dataBits, toIPStopBits(stopBits));
            }
            catch (Exception e)
            {
                // 
            }
        }

        public void Dispose()
        {
            sp_.Dispose();
        }

        public void Open()
        {
            try
            {
                sp_.Open();
            }
            catch (Exception e)
            {
                // reopen
            }
        }

        public void Close()
        {
            try
            {
                sp_.Close();
            }
            catch (Exception e)
            {
                // Reclose
            }
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            int ret = 0;
            try
            {
                ret = sp_.Read(buffer, offset, count);
            }
            catch (Exception e)
            {
                // Reconnect_();
            }
            return ret;
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            try
            {
                sp_.Write(buffer, offset, count);
            }
            catch (Exception e)
            {
                // Reconnect_();
            }
        }

        private System.IO.Ports.Parity toIPParity(Parity parity)
        {
            return dictParity_[parity];
        }

        private System.IO.Ports.StopBits toIPStopBits(StopBits stopBits)
        {
            return dictStopBits_[stopBits];
        }

    }
}
