using Microsoft.Extensions.Logging;
using MyDiagnostics.Logging;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.Serialization.Formatters;
using System.Text;
using WealthLab.Backtest;
using WealthLab.Core;
using WealthLab.Indicators;
using WealthLab.MyIndicators;
using static LoadWinLossCsv.LoadWinLossCsvDriver;


namespace LoadWinLossCsv
{
    public class LoadWinLossCsvDriver : UserStrategyBase
    {
        public static string WinLossCsvFilePath = @"C:\MyWealthLab\CompIdxOU\WealthLabLogs\Inputs\SessionLog_Win_Loss_Results_2025_08_04.csv";
        public static string ParameterFilePath = @"C:\MyWealthLab\CompIdxOU\WealthLabLogs\Inputs\LoadWinLossCsvParameters.csv";

        private static bool printErrorLog = false;

        private static StrategyParameters _cachedParameters;
        private static readonly object _parameterLock = new();

        private static bool _parametersInitialized = false;

        private static Dictionary<string, WinLossSummary> _summaryCache;
        private static readonly object _cacheLock = new();

        private static bool _summaryInitialized = false;

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

        /*       public static List<WinLossSummary> LoadSummaries()
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
               }*/

        public static TradeEvaluationResult ShouldExecuteTrade(string symbol)
        {
            InitializeParameters(ParameterFilePath);
            EnsureSummaryCacheInitialized();                     
            
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

        /*       public static StrategyParameters LoadStrategyParameters(string filePath)
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
               }*/

        public static StrategyParameters LoadStrategyParameters(string filePath)
        {
            lock (_parameterLock)
            {
                if (_cachedParameters != null)
                    return _cachedParameters;

                LogBuffer.Write($"[Read LoadStrategyParameter] from: '{filePath}'.");

                if (!File.Exists(filePath))
                    throw new FileNotFoundException($"Parameter file not found: {filePath}");

                var line = File.ReadLines(filePath).Skip(1).FirstOrDefault();
                if (line == null)
                    throw new InvalidDataException("Parameter file is empty or malformed.");

                var fields = line.Split(',');

                _cachedParameters = new StrategyParameters
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

                return _cachedParameters;
            }
        }

        public static void InitializeParameters(string filePath)
        {
            if (_parametersInitialized) return;

  //          ClearParameterCache(); // Clear stale or previous session data
            LoadStrategyParameters(filePath); // Triggers caching
            _parametersInitialized = true;

            if (printErrorLog)
                LogBuffer.Write("[Init] StrategyParameters initialized.");
        }

        public static void ClearParameterCache()
        {
            lock (_parameterLock)
            {
                _cachedParameters = null;
   //             _parametersInitialized = false; // Optional: reset flag if needed
                LogBuffer.Write("[Cache] StrategyParameters cache cleared.");
            }
        }


        public static WinLossSummary GetSummary(string symbol)
        {
  //          InitializeSummaryCache();

            if (_summaryCache.TryGetValue(symbol, out var summary))
            {
                return new WinLossSummary
                {
                    Symbol = summary.Symbol,
                    TotalTrades = summary.TotalTrades,
                    WinPercent = summary.WinPercent,
                    WinLossAmtRatio = summary.WinLossAmtRatio
                };
            }

            return new WinLossSummary
            {
                Symbol = symbol,
                TotalTrades = 0,
                WinPercent = 0.0,
                WinLossAmtRatio = 0.0
            };
        }

        /*       public static void InitializeSummaryCache()
               {
                   lock (_cacheLock)
                   {
                       if (_summaryCache != null) return; // already loaded

                       _summaryCache = new Dictionary<string, WinLossSummary>(StringComparer.OrdinalIgnoreCase);

                       if (!File.Exists(WinLossCsvFilePath))
                           return;

                       foreach (var line in File.ReadLines(WinLossCsvFilePath).Skip(1))
                       {
                           var fields = line.Split(',');

                           var summary = new WinLossSummary
                           {
                               Symbol = fields[0],
                               TotalTrades = int.TryParse(fields[3], out int trades) ? trades : 0,
                               WinPercent = double.TryParse(fields[4], out double winPct) ? winPct : 0.0,
                               WinLossAmtRatio = double.TryParse(fields[8], out double amtRatio) ? amtRatio : 0.0
                           };

                           _summaryCache[summary.Symbol] = summary;
                       }

                       if (printErrorLog)
                           LogBuffer.Write($"[Cache] Loaded {_summaryCache.Count} summaries into cache.");
                   }
               } */

        public static void EnsureSummaryCacheInitialized()
        {
            if (_summaryInitialized) return;

  //          ClearSummaryCache();
            InitializeSummaryCache();
        }


        public static void InitializeSummaryCache()
        {
            lock (_cacheLock)
            {
                if (_summaryInitialized) return;

                _summaryCache = new Dictionary<string, WinLossSummary>(StringComparer.OrdinalIgnoreCase);

                if (!File.Exists(WinLossCsvFilePath))
                    return;

                foreach (var line in File.ReadLines(WinLossCsvFilePath).Skip(1))
                {
                    var fields = line.Split(',');

                    var summary = new WinLossSummary
                    {
                        Symbol = fields[0],
                        TotalTrades = int.TryParse(fields[3], out int trades) ? trades : 0,
                        WinPercent = double.TryParse(fields[4], out double winPct) ? winPct : 0.0,
                        WinLossAmtRatio = double.TryParse(fields[8], out double amtRatio) ? amtRatio : 0.0
                    };

                    _summaryCache[summary.Symbol] = summary;
                }

                _summaryInitialized = true;

                if (printErrorLog)
                    LogBuffer.Write($"[Cache] Loaded {_summaryCache.Count} summaries into cache.");
            }
        }

        public static void ClearSummaryCache()
        {
            lock (_cacheLock)
            {
                _summaryCache = null;
      //          _summaryInitialized = false;

                if (printErrorLog)
                    LogBuffer.Write("[Cache] Summary cache cleared.");
            }
        }

        public static void NotifyNewSubmission()
        {
            ClearParameterCache();
            ClearSummaryCache();
            _summaryInitialized = false;
            _parametersInitialized = false; // Optional: reset flag if needed
        }
    }
}


   