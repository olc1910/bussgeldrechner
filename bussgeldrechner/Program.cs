using System;
using System.Linq;
using System.Threading;

namespace bussgeldrechner
{
    internal class Program
    {
        private static bool isStopRequested = false;
        private static uint UIspeed, UImax_speed;
        private static bool UIinort, UItol;
        private static int FatalDataException = 0;
        static void Main(string[] args)
        {
            Console.WriteLine("Bußgeldrechner - programmiert von Jesse Beimgraben, Otis Crossley, Jakub Bergmann");

            if (!Bussgeld.LoadData())
            {
                Console.Write("Error loading local File, fetching data from web..");
                var dl = Bussgeld.GetWebData();
                while (!dl.Wait(500))
                {
                    Console.Write(".");
                }
            }

            while (!isStopRequested)
            {
                FatalDataException = 0;

                UIspeed = GetUint("\nGeben Sie ihre Gefahrene Geschwindigkeit ein: ");
                UImax_speed = GetUint("\nGeben Sie die Zulässige Höchstgeschwindigkeit ein: ");
                CheckSpeedIntegrity();

                UIinort = GetBool("\nWaren Sie Innerorts? (J/N): ");
                UItol = GetBool("\nSoll die Toleranz mitberechnet werden (J/N): ");

                LetUserCheck();
                CompareToData();
            }
        }
        private static uint GetUint(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                if (uint.TryParse(Console.ReadLine(), out var UInt))
                    return UInt;
                
                Console.Write("\nBitte geben Sie eine valide Zahl ein");
            }
        }

        private static bool GetBool(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                var input = Console.ReadLine()?.ToUpper();
                if (input == "J") return true;
                if (input == "N") return false;

                Console.Write("\nBitte einfach nur J für Ja oder N für Nein eingeben");
            }
        }

        private static void CheckSpeedIntegrity()
        {
            while(UIspeed <= UImax_speed)
            {
                Console.Write("\nInput Error: Die Gefahrene Geschwindigkeit ist kleiner gleich der Höchstgeschwindigkeit. Bitte korrekte Werte eingeben.");
                UIspeed = GetUint("\nGeben Sie ihre Gefahrene Geschwindigkeit ein: ");
                UImax_speed = GetUint("\nGeben Sie die Zulässige Höchstgeschwindigkeit ein: ");
            }
        }

        private static void LetUserCheck()
        {
            while (true)
            {
                Console.WriteLine($"\nIhre eingebenen Werte sind:\nGefahren: {UIspeed}\nZugelassen: {UImax_speed}\nOrtsbereich: {(UIinort ? "innerorts" : "außerorts")}\nToleranz: {(UItol ? "Ja" : "Nein")}");
                Console.Write("\nWenn diese Werte Korrekt sind Pressen Sie Enter, ansonsten geben Sie bitte eine Zahl ein um ihren Wert zu ändern (1 - Gefahren, 2 - Zugelassen, 3 - Ortsbereich, 4 - Toleranz) ");
                var input = Console.ReadLine();

                switch (input)
                {
                    case "1":
                        UIspeed = GetUint("\nGeben Sie ihre Gefahrene Geschwindigkeit ein: ");
                        CheckSpeedIntegrity();
                        break;
                    case "2":
                        UImax_speed = GetUint("\nGeben Sie die Zulässige Höchstgeschwindigkeit ein: ");
                        CheckSpeedIntegrity();
                        break;
                    case "3":
                        UIinort = GetBool("\nWaren Sie Innerorts? (J/N): ");
                        break;
                    case "4":
                        UItol = GetBool("\nSoll die Toleranz mitberechnet werden (J/N): ");
                        break;
                    default: return;
                }
            }
        }

        private static void CompareToData()
        {
            var diff = calculate(UIspeed, UImax_speed, UItol, UIinort);
            if(diff < 1)
            {
                Console.Write("\nDu hast Glück gehabt und deine Überschreitung nach allen Abzügen beträgt unter 1 Km/h");
                Console.Write("\nWenn Sie eine erneute Berechnung durchführen möchten gebn Sie J ein oder drücken Sie Enter wenn das programm geschlossen werden soll: ");
                if (Console.ReadLine()?.ToUpper() != "J")
                {
                    isStopRequested = true;
                    return;
                }
                Console.Clear();
                return;
            }

            var data = Bussgeld.Bussgelddata.FirstOrDefault(d => d.innerorts == UIinort && diff >= d.von_kmh && diff <= d.bis_kmh);

            if(data == null)
            {
                FatalDataException++;
                Console.WriteLine("\nError: No fitting Values found, recovery initilized");
                RecoverFatal();
            }

            Console.WriteLine($"\nIhre Konsequenzen laut BKAtV:\nBußgeld: {data.Strafe}EUR, Punkte: {data.Punkte}, Fahrverbot: {data.fahrverbot} Monat(e)");
            Console.Write("\nWenn Sie eine erneute Berechnung durchführen möchten geben Sie J ein oder drücken Sie Enter wenn das programm geschlossen werden soll: ");
            if(Console.ReadLine()?.ToUpper() != "J")
            {
                isStopRequested = true;
                return;
            }
            Console.Clear();
        }

        private static int calculate(uint speed, uint max_speed, bool toleranz, bool innerorts)
        {
            var diff = (int)(speed - max_speed);
            if (!toleranz) return diff;

            if (toleranz && (innerorts || speed < 100)) return diff - 3;

            diff -= (int)(speed * 0.03);
            return diff;
        }

        private static void RecoverFatal() 
        {
            if(FatalDataException > 3)
            {
                Console.WriteLine("\nCould not recover, Exiting");
                Thread.Sleep(2500);
                Environment.Exit(-1);
            }
            Bussgeld.Bussgelddata = null;
            Bussgeld.GetWebData().GetAwaiter().GetResult();
            CompareToData();
        }
    }
}
