using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net;

namespace MatchHistory
{
    class Program
    {
        static Int64 sequenceNumber = -1;
        static string archivePath = AppDomain.CurrentDomain.BaseDirectory + "archive";
        static int errors = 0;
        static void Main()
        {
            Directory.CreateDirectory(Program.archivePath);
            Program.GetCurrentArchive();
            Console.WriteLine("Starting at Sequence Number = " + sequenceNumber);
            while (Program.sequenceNumber < Int64.MaxValue)
            {
                Program.RequestPage();
            }
            Console.ReadLine();
        }
        static void RequestPage()
        {
            using (WebClient request = new WebClient())
            {
                try
                {
                    Int64 floor = Program.sequenceNumber + 1;
                    Int64 ceiling = floor;
                    string fileName;
                    string response = request.DownloadString(@"http://api.steampowered.com/IDOTA2Match_570/GetMatchHistoryBySequenceNum/v0001/?key=XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX&start_at_match_seq_num=" + floor);
                    string pattern = @"""match_seq_num"":\s*(?<seq>\d+),";
                    Regex search = new Regex(pattern, RegexOptions.IgnoreCase);
                    MatchCollection matches = search.Matches(response);
                    if (matches.Count > 0)
                    {
                        foreach (Match match in matches)
                        {
                            GroupCollection groups = match.Groups;
                            Int64 current = Convert.ToInt64(groups["seq"].ToString());
                            ceiling = (ceiling < current ) ? current : ceiling;
                        }
                    }
                    fileName = floor + "-" + ceiling + ".json";
                    File.WriteAllText(Program.archivePath + "\\" + fileName , response);
                    Program.sequenceNumber = ceiling;
                    Console.Write((Program.errors > 0?Environment.NewLine:"") + fileName + Environment.NewLine);
                    Program.errors = 0;
                }
                catch (WebException e)
                {
                    Program.errors++;
                    System.Threading.Thread.Sleep(5000);
                    Console.Write("\r" + e.Message + "{" + Program.errors + "}");
                }
            }
        }


        static void GetCurrentArchive()
        {
            string[] files = Directory.GetFiles(Program.archivePath, "*.json").Select(path => Path.GetFileName(path)).ToArray();
            string pattern = @"\d+-(?<max>\d+).json";
            Regex search = new Regex(pattern, RegexOptions.IgnoreCase);
            for (int i = 0; i < files.Length; i++)
            {
                Int64 ceiling = Convert.ToInt64(search.Match(files[i]).Groups["max"].ToString());
                Program.sequenceNumber = ( Program.sequenceNumber < ceiling ) ? ceiling : Program.sequenceNumber;
            }
        }
    }
}
