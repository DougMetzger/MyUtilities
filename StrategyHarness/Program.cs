using CompIdxOverUnder;
using System;
using System.IO;
using WealthLab.Backtest;
using WealthLab.Core;
using WealthLab.Data;

namespace StrategyHarness
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Load symbol list
            var symbols = File.ReadAllLines("Symbols.txt");

            // Frequency selection
            Console.Write("Choose frequency (Daily/Weekly/Monthly): ");
            string input = Console.ReadLine()?.Trim().ToLower();
            Frequency freq = input switch
            {
                "weekly" => Frequency.Weekly,
                "monthly" => Frequency.Monthly,
                _ => Frequency.Daily
            };

            // Setup scale and dates
            var scale = new HistoryScale(freq);
            DateTime endDate = DateTime.Today;
            DateTime startDate = endDate.AddYears(-3); // customizable

            // Connect to IQFeed
            var provider = DataProviderFactory.Instance.Find("IQFeed");
            var runner = new StrategyRunner();

            foreach (var symbol in symbols)
            {
                try
                {
                    var bars = provider.GetHistory(symbol.Trim(), scale, startDate, endDate, 0, HistoryOptions.Default);
                    if (bars == null || bars.Count == 0)
                    {
                        Console.WriteLine($"⚠️ No data found for {symbol}. Skipping.");
                        continue;
                    }

                    var strategy = new CompIdxOU(bars);
                    var result = runner.RunBacktest(strategy);

                    Console.WriteLine($"\n📊 {symbol} [{freq}]: Net Profit = {result.Metrics.NetProfit:N2}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error with {symbol}: {ex.Message}");
                }
            }

            Console.WriteLine("\n✅ All backtests complete.");
        }
    }
}
