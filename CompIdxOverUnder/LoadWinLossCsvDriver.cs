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
        public LoadWinLossCsvDriver()
        {
            AddParameter("EvaluationMode", ParameterType.Int32, 0, 0, 2, 1);    //0    
        }

        public static string WinLossCsvFilePath = @"C:\MyWealthLab\CompIdxOU\WealthLabLogs\SessionLog_Win_Loss_Results_2025_08_04.csv";
       
        private int evaluationMode = 0;

        public enum EvaluationMode
        {
            Conservative,
            Moderate,
            Aggressive,
            //          SymbolSpecific,
            //         CustomThreshold
        }

        private EvaluationMode mode;

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
            public EvaluationMode EvaluationMode { get; set; }
            public bool ShouldTrade { get; set; }
            public WinLossSummary Summary { get; set; }
        }


        public static List<WinLossSummary> LoadSummaries()
        {
            LogBuffer.Write($"[Nav] Entered Load Summaries");

            var summaries = new List<WinLossSummary>();

            if (!File.Exists(WinLossCsvFilePath))
                return summaries;

            foreach (var line in File.ReadLines(WinLossCsvFilePath).Skip(1)) // skip header
            {
                var fields = line.Split(',');

                if (fields.Length < 9)
                    continue; // skip malformed rows

                var summary = new WinLossSummary
                {
                    Symbol = fields[0],
                    TotalTrades = int.TryParse(fields[3], out int trades) ? trades : 0,
                    WinPercent = double.TryParse(fields[4], out double winPct) ? winPct : 0.0,
                    WinLossAmtRatio = double.TryParse(fields[8], out double amtRatio) ? amtRatio : 0.0
                };

                Debug.WriteLine($"[Load Win/Loss Data] Symbol={fields[0]}, TradeCount={trades},  WinPct={winPct}, AmtRation={amtRatio}");

                summaries.Add(summary);
            }

            return summaries;
        }

        public override void Execute(BarHistory bars, int idx)
        {
            LogBuffer.Write($"[Nav] Entered Execute");

            if (bars.Count < 2) // Arbitrary gate to ensure it's done once
            {
                LoadParametersOnce();
            }

            WriteToDebugLog($"LoadWinLossCsvDriver:Execute");

            var summary = LoadWinLossCsvDriver.GetSummary(bars.Symbol);

            var evalResult = LoadWinLossCsvDriver.ShouldExecuteTrade(bars.Symbol, mode);

            Debug.WriteLine($"[Trade Evaluation] Symbol={evalResult.Symbol}, Mode={evalResult.EvaluationMode},  Result={evalResult.ShouldTrade}, Reason={evalResult.Summary}");


            if (!evalResult.ShouldTrade)
            {
                return;
            }

        }

        private void LoadParametersOnce()
        {
            LogBuffer.Write($"[Nav] Entered LoadParametersOnce");
            evaluationMode = Parameters[0].AsInt;

            mode = (EvaluationMode)evaluationMode;
        }

        public static WinLossSummary GetSummary(string symbol)
        {
            LogBuffer.Write($"[Nav] Entered Get Summary");

            var summary = LoadSummaries().FirstOrDefault(s =>
                s.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));

            if (summary == null)
            {
                Debug.WriteLine($"[LoadWinLossCsvDriver] CSV entry not found for symbol '{symbol}'.");
                // Optionally create a default summary object with 0 trades
                return new WinLossSummary
                {
                    Symbol = symbol,
                    TotalTrades = 0,
                    WinPercent = 0.0,
                    WinLossAmtRatio = 0.0
                };
            }

            return summary;
        }

        public static TradeEvaluationResult ShouldExecuteTrade(string symbol, EvaluationMode mode)
        {
            LogBuffer.Write($"[Nav] Entered ShouldExecuteTrade for symbol '{symbol}', mode '{mode}'.");

            var summary = GetSummary(symbol); // Reads CSV internally each time
            bool shouldTrade = false;
            mode = EvaluationMode.Conservative;


            switch (mode)
            {
                case EvaluationMode.Conservative:
                    shouldTrade = summary.TotalTrades >= 2 &&
                                  summary.WinPercent >= .55 &&
                                  summary.WinLossAmtRatio >= 3;
                    LogBuffer.Write($"[Should Execute Traded] for  symbol '{symbol}', mode '{mode}'.");
                    LogBuffer.Write($"[Criteria:] mode {EvaluationMode.Conservative}, tt '{summary.TotalTrades}', wp '{summary.WinPercent}', ratio '{summary.WinLossAmtRatio}'.");
                    break;

                case EvaluationMode.Moderate:
                    shouldTrade = summary.TotalTrades >= 2 &&
                                  summary.WinPercent >= .45 &&
                                  summary.WinLossAmtRatio >= 2.5;
                    LogBuffer.Write($"[Should Execute Traded] for  symbol '{symbol}', mode '{mode}'.");
                    LogBuffer.Write($"[Criteria:] mode {EvaluationMode.Moderate}, tt '{summary.TotalTrades}', wp '{summary.WinPercent}', ratio '{summary.WinLossAmtRatio}'.");
                    break;

                case EvaluationMode.Aggressive:
                    shouldTrade = summary.TotalTrades >= 0 &&
                                  summary.WinPercent >= .0;
                    LogBuffer.Write($"[Should Execute Traded] for  symbol '{symbol}', mode '{mode}'.");
                    LogBuffer.Write($"[Criteria:] mode {EvaluationMode.Aggressive}, tt '{summary.TotalTrades}', wp '{summary.WinPercent}', ratio '{summary.WinLossAmtRatio}'.");

                    break;

                default:
                    Debug.WriteLine($"Unknown EvaluationMode: {mode}");
                    break;
            }

            return new TradeEvaluationResult
            {
                Symbol = symbol,
                EvaluationMode = mode,
                ShouldTrade = shouldTrade,
                Summary = summary
            };
        }


        public void RunCsvImport(string csvPath)
        {
            LogBuffer.Write($"[Nav] Entered RunCsvImport");

            CsvLoader.LoadCsv(csvPath);  // ✅ This is the integration point
        }
    }

    public class CsvLoader
    {
        public static void LoadCsv(string filePath)
        {
            LogBuffer.Write($"[Nav] Entered LoadCsvy");

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


   