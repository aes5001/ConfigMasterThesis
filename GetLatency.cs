using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;    
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;



namespace Test
{
    class Program
    {


        static void Main(string[] args)
        {
            List<string> externalPC_timeAndID= new List<string>();
            List<string> UE_timeAndID= new List<string>();

            //External PC
            using (var reader = new StreamReader(@"PathToCSV_from_Wireshark_Receiver")) //check that the fields in the .csv is the same as below, or update accordinly 
            {
                List<string> timeStamp = new List<string>();
                List<string> ID = new List<string>();

                (timeStamp, ID) = GetTimeUDP_ID_PC(reader);

                StringBuilder sbOutput = new StringBuilder();

                TextWriter tw = new StreamWriter(@".....timestamps_UE...csv");

                for (int i = 0; i < timeStamp.Count - 1; i++)
                {
                    var res = timeStamp[i] + "," + ID[i];
                    externalPC_timeAndID.Add(res);

                    tw.WriteLine(res);
                }
                tw.Close();
            }
            using (var reader = new StreamReader(@"...PathToCSV_from_Wireshark_Sender..."))
            {
                List<string> timeStamp = new List<string>();
                List<string> ID = new List<string>();

                (timeStamp, ID) = GetTimeUDP_ID_UE(reader);

                StringBuilder sbOutput = new StringBuilder();

                TextWriter twUE = new StreamWriter(@"....timestamps_UE1...."); //check that the fields in the .csv is the same as below, or update accordinly 

                for (int i = 0; i < timeStamp.Count - 1; i++)
                {
                    var res = timeStamp[i] + "," + ID[i];

                    UE_timeAndID.Add(res);

                    twUE.WriteLine(res);
                }
                twUE.Close();
            }

            GetLatency(externalPC_timeAndID, UE_timeAndID);
        }

        public static void GetLatency (List<string> externalPC_timeAndID, List<string> UE_timeAndID)
        {
            List<double> diff = new List<double>();
            List<double> allLatency = new List<double>();
            List<string> externalPCDate = new List<string>();
            List<string> AllSecs = new List<string>();
            List<string> PC_ID = new List<string>();
            List<string> UE_ID = new List<string>();
            List<TimeSpan> allTimesPC = new List<TimeSpan>();
            List<TimeSpan> allTimesUE = new List<TimeSpan>();
            DateTime datePC = new DateTime();
            DateTime dateUE = new DateTime();

            string dateStringEx;

            foreach (var entry in externalPC_timeAndID)
            {

                var values = entry.Split(',');

                dateStringEx = Regex.Match(values[0], @"\d+([\:]\d+)([\:]\d+)([\:]\d+)?").Value;
                externalPCDate.Add(dateStringEx);

                var newTime = values[0].Remove(values[0].Count() - 3);
                string completeDateTime = "01-08-2019" + " " + newTime; //just added some date to make the format correct as no tests are conducted over multiple days --Needs to be same date as UE

                datePC = DateTime.ParseExact(completeDateTime, "dd-MM-yyyy HH:mm:ss.ffffff", null);
                allTimesPC.Add(datePC.TimeOfDay);

                PC_ID.Add(values[1]);

            }

            foreach (var entry in UE_timeAndID)
            {
                var values = entry.Split(',');

                var newTime = values[0].Remove(values[0].Count() - 3);
                string test = "01-08-2019" + " " + newTime; //just added some date to make the format correct as no tests are conducted over multiple days --Needs to be same date as PC

                dateUE = DateTime.ParseExact(test, "dd-MM-yyyy HH:mm:ss.ffffff", null);
                allTimesUE.Add(dateUE.TimeOfDay);

                UE_ID.Add(values[1]);

            }
            
            double median = 0;

            List<double> medianLatencyPerSec = new List<double>();

            TimeSpan span = new TimeSpan();

            for (int i = 0; i < PC_ID.Count - 1; i++)
            {
                if (i < allTimesPC.Count && i < allTimesUE.Count)
                {
                    double latency = 0;

                    var ueID = UE_ID[i];
                    var pcID = PC_ID[i];

                    if (ueID.Equals(pcID, StringComparison.OrdinalIgnoreCase))
                    {
                        span = allTimesPC[i] - allTimesUE[i];
                        latency = (int)span.TotalMilliseconds+40;

                        if (latency < 0)
                        {

                        }
                        PC_ID[i] = "used";
                    }
                    else
                    {
                        if (PC_ID.Contains(UE_ID[i]))
                        {
                            ueID = UE_ID[i];
                            int idx = PC_ID.IndexOf(ueID);

                            if ((i + 100 > idx))
                            {
                                PC_ID[idx] = "used";

                                span = allTimesPC[idx] - allTimesUE[i];
                                latency = (int)span.TotalMilliseconds+40;
                                if (latency < 0)
                                {

                                }
                            }
                        }
                    }

                    if (latency != 0)
                    {
                        allLatency.Add(latency);
                        diff.Add(latency);
                    }

                    if (externalPCDate[i] != externalPCDate[i + 1])
                    {
                        if (diff.Count > 0)
                        {
                            double[] sourceNumbers = diff.ToArray();
                            median = GetMedian(sourceNumbers);
                            var test = median;

                            if (median != 0)
                            {
                                medianLatencyPerSec.Add(median);
                            }
                        }
                        diff.Clear();
                    }
                }
            }
            TextWriter tw2 = new StreamWriter(@"....LATENCY_perSec_UE1....");
            foreach (double s in medianLatencyPerSec)
                tw2.WriteLine(s);


            tw2.Close();

            TextWriter tw3 = new StreamWriter(@"....LATENCY_all_UE....");
            foreach (double s in allLatency)
                tw3.WriteLine(s);


            tw3.Close();
        }

        public static (List<string>time, List<string>id) GetTimeUDP_ID_PC(StreamReader reader)
        {

            List<string> time = new List<string>();

            List<string> dest = new List<string>();
            List<string> timeStamp = new List<string>();
            List<string> source = new List<string>();
            List<string> protocol = new List<string>();
            List<string> portNum = new List<string>();
            List<string> ID = new List<string>();
            List<string> IDres = new List<string>();

            var counter = 0;
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line.Split(',');

                if (counter > 0)
                {
                    string destResString = Regex.Match(values[2], @"\d+([\.]\d+)([\.]\d+)([\.]\d+)?").Value;
                    string port = Regex.Match(values[4], @"\d+").Value;
                    string timeRes = Regex.Match(values[7], @"\d+[\:]\d+[\:]\d+[\.]\d+").Value;
                    string sourceResString = Regex.Match(values[1], @"\d+([\.]\d+)([\.]\d+)([\.]\d+)?").Value;
                    string IDstring = Regex.Match(values[5], @"\([^\d]*(\d+)[^\d]*\)").Value;

                    time.Add(timeRes);
                    dest.Add(destResString);
                    source.Add(sourceResString);
                    protocol.Add(values[3]);
                    portNum.Add(port);
                    ID.Add(IDstring);

                }

                counter++;
            }

            for (int i = 0; i < time.Count; i++)
            {
                //Change for each UE and update IP addresses and ports according to sender and receiver

                //if (dest[i] == "152.94.122.104" && source[i] == "152.94.122.170" && protocol[i].Contains("UDP"))
               if (dest[i] == "152.94.122.104" && source[i] == "152.94.122.170" && protocol[i].Contains("UDP") && portNum[i] == "1001")
               //if (dest[i] == "172.16.0.20" && source[i] == "172.16.0.1" && protocol[i].Contains("UDP") )
                //if (dest[i] == "152.94.122.104" && source[i] == "152.94.122.194" && protocol[i].Contains("UDP") && portNum[i] == "1010")
                {
                    timeStamp.Add(time[i]);
                    IDres.Add(ID[i]);
                }
            }
            return (timeStamp, IDres);
        }

        public static (List<string> time, List<string> id) GetTimeUDP_ID_UE(StreamReader reader)
        {

            List<string> time = new List<string>();
            List<string> dest = new List<string>();
            List<string> timeStamp = new List<string>();
            List<string> source = new List<string>();
            List<string> protocol = new List<string>();
            List<string> portNum = new List<string>();
            List<string> ID = new List<string>();
            List<string> IDres = new List<string>();

            var counter = 0;
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line.Split(',');

                if (counter > 0)
                {
                    string destResString = Regex.Match(values[2], @"\d+([\.]\d+)([\.]\d+)([\.]\d+)?").Value;
                    string port = Regex.Match(values[4], @"\d+").Value;
                    string timeRes = Regex.Match(values[7], @"\d+[\:]\d+[\:]\d+[\.]\d+").Value;
                    string sourceResString = Regex.Match(values[1], @"\d+([\.]\d+)([\.]\d+)([\.]\d+)?").Value;
                    string IDstring = Regex.Match(values[5], @"\([^\d]*(\d+)[^\d]*\)").Value;

                    time.Add(timeRes);
                    dest.Add(destResString);
                    source.Add(sourceResString);
                    protocol.Add(values[3]);
                    portNum.Add(port);

                    ID.Add(IDstring);

                }

                counter++;
            }

            for (int i = 0; i < time.Count; i++)
            {
                //Change for each UE and update IP addresses according to sender and receiver
                if (dest[i] == "152.94.122.104" && source[i]=="172.16.0.2" && protocol[i].Contains("UDP"))
                //if (dest[i] == "172.16.0.20" && source[i]=="172.16.0.1" && protocol[i].Contains("UDP"))
                {
                    timeStamp.Add(time[i]);
                    IDres.Add(ID[i]);
                }
            }
            return (timeStamp, IDres);
        }

        public static double GetMedian(double[] sourceNumbers)
        {
            //Framework 2.0 version of this method. there is an easier way in F4        
            if (sourceNumbers == null || sourceNumbers.Length == 0)
                throw new System.Exception("Median of empty array not defined.");

            //make sure the list is sorted, but use a new array
            double[] sortedPNumbers = (double[])sourceNumbers.Clone();
            Array.Sort(sortedPNumbers);

            //get the median
            int size = sortedPNumbers.Length;
            int mid = size / 2;
            double median = (size % 2 != 0) ? (double)sortedPNumbers[mid] : ((double)sortedPNumbers[mid] + (double)sortedPNumbers[mid - 1]) / 2;
            return median;
        }
      
    }
}
