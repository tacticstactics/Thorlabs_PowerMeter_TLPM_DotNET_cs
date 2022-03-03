
namespace PM103_Scope_Sample_SW_Trigger
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text;
    using Thorlabs.TLPM_32.Interop;

    class Program
    {
        static void Main(string[] args)
        {
            HandleRef Instrument_Handle = new HandleRef();

            TLPM searchDevice = new TLPM(Instrument_Handle.Handle);

            uint count = 0;

            string firstPowermeterFound = "";

            try
            {
                int pInvokeResult = searchDevice.findRsrc(out count);

                if (count > 0)
                {
                    StringBuilder descr = new StringBuilder(1024);

                    searchDevice.getRsrcName(0, descr);

                    firstPowermeterFound = descr.ToString();
                }
            }
            catch { }

            if (count == 0)
            {
                searchDevice.Dispose();
                Console.WriteLine("No power meter could be found.");
                return;
            }

            //always use true for ID Query
            TLPM device = new TLPM(firstPowermeterFound, true, false);  //  For valid Ressource_Name see NI-Visa documentation.

            //Start autoset
            autoset(device);

            device.confCurrentMeasurementSequence(1);

            bool startSuccess = false;

            try
            {
                uint autoTriggerDelay = 0;
                bool triggerForced;
                device.startMeasurementSequence(autoTriggerDelay, out triggerForced);

                startSuccess = true;
            }
            catch { }

            int baseTime = 10;
            int dataSize = baseTime * 100;

            // The size of the array is always baseTime * 100
            //the valid range for base time is from 1 to 100. 
            float[] timeStamps = new float[dataSize]; // time stamps in milliseconds
            float[] currentData = new float[dataSize]; // current in A

            if (startSuccess)
            {
                device.getMeasurementSequence(10, timeStamps, currentData);

                string directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                string delimiter = ";";

                string path = directory + "//CurrentData.csv";

                using (StreamWriter outStream = new StreamWriter(path, false, Encoding.UTF8))
                {
                    for (int i = 0; i < dataSize; i++)
                    {
                        CultureInfo cultInfo = CultureInfo.InstalledUICulture;
                        outStream.WriteLine(timeStamps[i].ToString("F3", cultInfo) + delimiter + currentData[i].ToString("G5", cultInfo));
                    }
                }

                Console.WriteLine("Measurement SW Trigger data:");
                Console.WriteLine(path);

                if (File.Exists(path))
                {
                    try
                    {
                        System.Diagnostics.Process.Start(path);
                    }
                    catch { }
                }
            }
            else
            {
                Console.WriteLine("Measurement SW Trigger couldnt read any data.");
            }

            device?.Dispose();

            Console.ReadKey();
        }

        private static void autoset(TLPM device)
        {
            //Set to PEAK mode for peak search/ autoset
            device.setFreqMode(1);

            device.startPeakDetector();

            System.Threading.Thread.Sleep(1000);

            bool isRunning = true;

            while (isRunning)
            {
                device.isPeakDetectorRunning(out isRunning);
            }

            //Set to CW mode for normal measurement
            device.setFreqMode(0);
        }
    }
}
