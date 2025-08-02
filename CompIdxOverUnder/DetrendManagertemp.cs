using CompIdxOverUnderDriver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WealthLab.Backtest;
using WealthLab.Core;
using WealthLab.Indicators;
using WealthLab.MyIndicators;

namespace CompIdxOverUnderDriver
{
    public class DetrendManager    {
       
        public SMA smaDetrendFast { get; private set; }
        public SMA smaDetrendSlow { get; private set; }
    //  public SMA smaDetrend { get; private set; }
        Dictionary<string, Dictionary<int, double>> smaDetrend = new();

        public int detrendPeriodFast = 1;
        public int detrendPeriodBar = 1;
        public double value; 



        public DetrendManager(BarHistory bars, int detrendPeriodSlow, bool enableDebugLogging)
        {

            smaDetrendFast = new SMA(bars.Close, detrendPeriodFast);
            smaDetrendSlow = new SMA (bars.Close, detrendPeriodSlow);
      //    smaDetrend = new SMA (bars.Close, detrendPeriodBar);
                

            if (enableDebugLogging)
            {
                WLLogger.Write("Detrend Indicator");
            }

            for (int i = detrendPeriodSlow; i < bars.Count; i++)
{
    value = smaDetrendFast[i] - smaDetrendSlow[i];

    // Ensure the symbol key exists
    if (!smaDetrend.ContainsKey(bars.Symbol))
        smaDetrend[bars.Symbol] = new Dictionary<int, double>();

    // Store the detrend value for current index
    smaDetrend[bars.Symbol][i] = value;

    if (enableDebugLogging)
    {
        WLLogger.Write("Symbol: " + bars.Symbol +
                       " dtrendFast: " + smaDetrendFast[i].ToString("#.00") +
                       " detrendSlow: " + smaDetrendSlow[i].ToString("#.00") +
                       " detrendValue: " + value.ToString("#.00"));
    }
}
        }

        public void PlotDetrend(UserStrategyBase strategy)
        {
            // Retrieve symbol-specific dictionary
            if (smaDetrend.TryGetValue(strategy.Bars.Symbol, out var detrendDict))
            {
                Series detrendSeries = new Series("Detrend");

                foreach (var kvp in detrendDict)
                {
                    detrendSeries[kvp.Key] = kvp.Value;
                }

                strategy.PlotIndicator(detrendSeries, WLColor.Blue, PlotStyle.Line, false, "Detrend");
                strategy.DrawHorzLine(0, WLColor.Black, 1, LineStyle.Solid, "Detrend");
            }
            else if (enableDebugLogging)
            {
                WLLogger.Write("No detrend data found for symbol: " + strategy.Bars.Symbol);
            }
        }

        public bool TryGetDetrendValue(string symbol, int idx, out double detrendValue)
        {
            {
                detrendValue = 0.0;

                if (smaDetrend.TryGetValue(symbol, out var innerDict))
                {
                    if (innerDict.TryGetValue(idx, out value))
                    {
                        return true;
                    }
                }

                return false;
            }
            
        }

    }
}
