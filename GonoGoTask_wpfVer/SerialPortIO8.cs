using System;
using System.Windows;
using System.IO.Ports;
using System.Threading;

namespace GonoGoTask_wpfVer
{
    class SerialPortIO8
    {
        public SerialPortIO8()
        {
        }

        public static string Locate_serialPortIO8()
        {/* 
            Locate the correct COM port used for communicating with the DLP-IO8 

            Returns:
                string serialPortIO8 Name, if located; "", false
            
             */

            string[] portNames = SerialPort.GetPortNames();
            string serialPortIO8_Name = "";
            foreach (string portName in portNames)
            {
                SerialPort serialPort = new SerialPort();
                try
                {
                    serialPort.PortName = portName;
                    serialPort.BaudRate = 115200;
                    serialPort.Open();

                    for (int i = 0; i < 5; i++)
                    {
                        // channel 1 Ping command of the DLP-IO8, return 0 or 1
                        serialPort.WriteLine("Z");

                        // Read exist Analog in from serialPort
                        string str_Read = serialPort.ReadExisting();

                        if(str_Read.Contains("V"))
                        {//
                            serialPortIO8_Name = portName;
                            serialPort.Close();
                            return serialPortIO8_Name;
                        }
                        Thread.Sleep(30);
                    }
                    serialPort.Close();
                }
                catch (Exception ex)
                {
                    if (serialPort.IsOpen)
                        serialPort.Close();
                    MessageBox.Show(ex.Message, "Error Message", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            return serialPortIO8_Name;
        }


        public static void Open_serialPortIO8(SerialPort serialPort_IO8, string portName, int baudRate)
        {
            try
            {
                serialPort_IO8.PortName = portName;
                serialPort_IO8.BaudRate = baudRate;
                serialPort_IO8.Open();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
