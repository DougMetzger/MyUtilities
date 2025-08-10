using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using WealthLab.Backtest;
using WealthLab.Core;
using WealthLab.Indicators;
using WealthLab.MyIndicators;
using MyDiagnostics.Logging;


namespace LoadWinLossCsv
{
    public class LoadWinLossCsvDriver : UserStrategyBase
    {
        public static string WinLossCsvFilePath = @"C:\MyWealthLab\CompIdxOU\WealthLabLogs\Inputs\SessionLog_Win_Loss_Results_2025_08_04.csv";
        public static string ParameterFilePath  = @"C:\MyWealthLab\CompIdxOU\WealthLabLogs\Inputs\Parameters.csv";

        private static bool printErrorLog = false;

        public class StrategyParameters
        {
            public string Strategy { get; set; }
            public string Parameter { get; set; }
            public string Version { get; set; }
            public int TotalTrades { get; set; }
            public double WinPercent { get; set; }
            public double Ratio { get; set; }
            public double BlendedCalc { get; set; }
            public string PrintErrorLog { get; set; }  
            public string BypassWinLossCheck { get; set; }
        }

        public class WinLossSummary
        {
            public string Symbol { get; set; }
            public int TotalTrades { get; set; }
            public double WinPercent { get; set; }
            public double WinLossAmtRatio { get; set; }
        }

        public class TradeEvaluationResult
        {
            public string Symbol { get; set; }
            public bool ShouldTrade { get; set; }
            public WinLossSummary Summary { get; set; }
        }

        public override void Execute(BarHistory bars, int idx)
        {
            if (printErrorLog)
            {
                WriteToDebugLog($"LoadWinLossCsvDriver:Execute");
            }                    
        }            

        public static List<WinLossSummary> LoadSummaries()
        {
            if (printErrorLog)
            {
                LogBuffer.Write($"Entered Load Summaries");
            }

            var summaries = new List<WinLossSummary>();

            if (!File.Exists(WinLossCsvFilePath))
                return summaries;

            foreach (var line in File.ReadLines(WinLossCsvFilePath).Skip(1)) // skip header
            {
                var fields = line.Split(',');

    //            if (fields.Length < 9)
     //               continue; // skip malformed rows

                var summary = new WinLossSummary
                {
                    Symbol = fields[0],
                    TotalTrades = int.TryParse(fields[3], out int trades) ? trades : 0,
                    WinPercent = double.TryParse(fields[4], out double winPct) ? winPct : 0.0,
                    WinLossAmtRatio = double.TryParse(fields[8], out double amtRatio) ? amtRatio : 0.0
                };

                Debug.WriteLine($"[Load Win/Loss Data] Symbol={summary.Symbol}, TradeCount={summary.WinPercent},  WinPct={summary.WinPercent}, AmtRatio={summary.WinLossAmtRatio}");

                summaries.Add(summary);
            }

            return summaries;
        }

        public static StrategyParameters LoadStrategyParameters(string filePath)
        {
    //        if (printErrorLog)
     //       {
                LogBuffer.Write($"[Read LoadStrategyParameter] from: '{filePath}'.");
   //         }
               
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Parameter file not found: {filePath}");

            var lines = File.ReadLines(filePath).Skip(1).FirstOrDefault(); // Skip header
            if (lines == null)
                throw new InvalidDataException("Parameter file is empty or malformed.");

            var fields = lines.Split(',');
  //          if (fields.Length < 6)
   //             throw new InvalidDataException("Parameter file does not contain expected fields.");

            return new StrategyParameters
            {
                Strategy = fields[0],
                Parameter = fields[1],
                Version = fields[2],
                TotalTrades = int.Parse(fields[3]),
                WinPercent = double.Parse(fields[4]),
                Ratio = double.Parse(fields[5]),
                BlendedCalc = double.Parse(fields[6]),
                PrintErrorLog = fields[7],
                BypassWinLossCheck = fields[8]
            };
        }
           

        public static WinLossSummary GetSummary(string symbol)
        {
            if (printErrorLog)
            {
                LogBuffer.Write($"Entered Get Summary for Symbol: '{symbol}'");
            }

            var summary = LoadSummaries().FirstOrDefault(s =>
                s.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));
                       
            {                
                return new WinLossSummary
                {
                    Symbol = summary.Symbol,
                    TotalTrades = summary.TotalTrades,
                    WinPercent = summary.WinPercent,
                    WinLossAmtRatio = summary.WinLossAmtRatio
                };
            }   
                        
        }

        public static TradeEvaluationResult ShouldExecuteTrade(string symbol)
        {
 
            var parameters = LoadStrategyParameters(ParameterFilePath);

            printErrorLog = false;

            if (parameters.PrintErrorLog == "t")
            {
                printErrorLog = true;
                LogBuffer.Write($"Entered ShouldExecuteTrade Symbol: '{symbol}'");
                LogBuffer.Write($"Parameters: TotalTrades '{parameters.TotalTrades}' WinPercent '{parameters.WinPercent}' WinLossAmtRatio '{parameters.Ratio}' BlendedCalc '{parameters.BlendedCalc}'.");
            }

            if (parameters.BypassWinLossCheck == "t")
            {
                if (printErrorLog)
                {
                    LogBuffer.Write($"Bypassing Win/Loss Check for Symbol: '{symbol}'");
                }

                return new TradeEvaluationResult
                {
      //              Symbol = symbol,
                    ShouldTrade = true,  //Bypass Win/Loss Check and always return true 
                };
            }

            var summary = GetSummary(symbol);

            double blendedCalc = (summary.WinPercent) * (summary.WinLossAmtRatio);
            blendedCalc = Math.Round(blendedCalc, 2);

            if (printErrorLog)
            {
                LogBuffer.Write($"Get Results: TotalTrades '{summary.TotalTrades}' WinPercent ' {summary.WinPercent}' WinLossAmtRatio '{summary.WinLossAmtRatio}' BlendedCalc '{blendedCalc}'.");
            }

            bool shouldTrade = summary.TotalTrades >= parameters.TotalTrades &&
                                       blendedCalc >= parameters.BlendedCalc;
                 //              summary.WinPercent >= parameters.WinPercent &&
                 //              summary.WinLossAmtRatio >= parameters.Ratio;
                             if (printErrorLog)
            {
                LogBuffer.Write($"Should Trade: '{shouldTrade}'");
            }                      

            return new TradeEvaluationResult
            {
 //               Symbol = symbol,
                ShouldTrade = shouldTrade,
 //               Summary = summary
            };
        }

        public void RunCsvImport(string csvPath)
        {
            if (printErrorLog)
            {
                LogBuffer.Write($"Entered RunCsvImport");
            }

            CsvLoader.LoadCsv(csvPath, printErrorLog);  // ✅ This is the integration point
        }
    }

    public class CsvLoader
    {
        public static void LoadCsv(string filePath, bool printErrorLog)
        {

            if (printErrorLog)
            {
                LogBuffer.Write($"Entered LoadCsv");
            }
             
            // ✅ This is where logging works—inside a method block
            LogBuffer.Write($"[CSV] Starting load from '{filePath}'.");

            try
            {
                foreach (var line in File.ReadLines(filePath))
                {
                    // Your parsing logic here
                    LogBuffer.Write($"[CSV] Parsed line: '{line}'");
                }

                LogBuffer.Write($"[CSV] Completed loading from '{filePath}'.");
            }
            catch (Exception ex)
            {
                LogBuffer.Write($"[CSV] ERROR: {ex.Message}");
                throw;
            }
        }
    }

}


   