using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;

namespace SerialRedirector
{
    class RobustSerial
    {
        private SerialPort sp_;
        private string portName_;
        private int baudRate_;
        private Parity parity_;
        private int dataBits_;
        private StopBits stopBits_;
        private int bytesToRead_;

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
            parity_ = parity;
            dataBits_ = dataBits;
            stopBits_ = stopBits;
            try
            {
                sp_ = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
            }
            catch (Exception e)
            {
                // 
            }
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
    }
}
