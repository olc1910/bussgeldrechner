using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace bussgeldrechner
{
    internal class Bussgeld
    {
        public class Data
        {
            public uint von_kmh { get; set; }
            public uint bis_kmh { get; set; }
            public bool innerorts;
            public int Strafe { get; set; }
            public int Punkte { get; set; }
            public int fahrverbot { get; set; }
        }

        public static List<Data> Bussgelddata = new List<Data>(); //O(n) hölle :skull:
        private static int ExceptionCounter = 0;
        private static readonly string path = Path.Combine(AppContext.BaseDirectory, "bussgeldkatalog_geschwindigkeit.csv"); 
        private static readonly HttpClient http = new HttpClient();
        public static bool LoadData()
        {
            try
            {
                if (!File.Exists(path)) return false;

                var lines = File.ReadAllLines(path);

                for (int i = 1; i < lines.Length; i++)
                {
                    var data = lines[i].Split(',');
                    Data d = new Data
                    {
                        innerorts = data[0] == "innerorts",
                        von_kmh = uint.Parse(data[1]),
                        bis_kmh = uint.Parse(data[2]),
                        Strafe = int.Parse(data[3]),
                        Punkte = int.Parse(data[4]),
                        fahrverbot = int.Parse(data[5])
                    };
                    Bussgelddata.Add(d);
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading data: {ex.Message}");
                return false;
            }
        }

        public static async Task GetWebData()
        {
            try
            {
                var html = await http.GetStringAsync("https://www.bussgeldinfo.org/bussgeldkatalog/geschwindigkeit/");

                var rx = new Regex(
                    @"class=""ortsangabe"">(.*?)</div>.*?" +
                    @"class=""kmh-bereich"">(.*?)</div>.*?" +
                    @"class=""betrag"">(.*?)</div>.*?" +
                    @"class=""menge"">(.*?)</div>.*?" +
                    @"class=""dauer"">(.*?)</div>",
                    RegexOptions.Singleline);

                string Clean(string s) => Regex.Replace(WebUtility.HtmlDecode(s), @"\s+", " ").Trim();
                var tempList = new List<Data>();
                foreach (Match m in rx.Matches(html))
                {
                    string ort = Clean(m.Groups[1].Value);
                    string kmh = Clean(m.Groups[2].Value);
                    string betrag = Clean(m.Groups[3].Value);
                    string punkte = Clean(m.Groups[4].Value);
                    string fahrverbot = Clean(m.Groups[5].Value);

                    var nums = Regex.Matches(m.Groups[2].Value, @"\d+").Cast<Match>()
                                    .Select(x => uint.Parse(x.Value))
                                    .ToArray();
                    
                    Data d = new Data
                    {
                        innerorts = ort == "innerorts",
                        von_kmh = nums[0],
                        bis_kmh = nums.Length > 1 ? nums[1] : uint.MaxValue,
                        Punkte = int.Parse(punkte),
                        Strafe = int.Parse(betrag.Replace("€", "")),
                        fahrverbot = (int) char.GetNumericValue(fahrverbot[0])
                    };
                    tempList.Add(d);
                }
                Bussgelddata.AddRange(tempList);
                _ = Task.Run(PopulateLocalTable);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching data from web: {ex.Message}");
                ExceptionCounter++;

                if (ex is HttpRequestException || ex is TaskCanceledException)
                {
                    if (ExceptionCounter > 3)
                    {
                        Console.WriteLine("Retries all failed, restart the application and/or look at your Network Connection");
                        return;
                    }
                    Console.WriteLine($"Retrying..");
                    await Task.Delay(1000);
                    await GetWebData();
                }
                else
                {
                    Console.WriteLine($"Closing programm shortly...");
                    await Task.Delay(3000);
                    Environment.Exit(-1);
                }
            }
        }

        private static void PopulateLocalTable()
        {
            try
            {
                var lines = new List<string> {"Ortsbereich,von_kmh,bis_kmh,Strafe,Punkte,Fahrverbot"};
                lines.AddRange(Bussgelddata.Select(d => $"{(d.innerorts ? "innerorts" : "außerorts")},{d.von_kmh},{d.bis_kmh},{d.Strafe},{d.Punkte},{d.fahrverbot}"));
                File.WriteAllLines(path, lines);
            }
            catch (Exception ex) {
                Console.WriteLine($"Error writing local table to {path} with msg: {ex.Message}");
            }
        }
    }
}
