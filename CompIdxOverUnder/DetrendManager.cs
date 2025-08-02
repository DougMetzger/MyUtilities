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
        public SMA smaDetrend { get; private set; }

        public int detrendPeriodFast = 1;
        public int detrendPeriodBar = 1;

        public DetrendManager(BarHistory bars, int detrendPeriodSlow, bool enableDebugLogging = false)
        {

            smaDetrendFast = new SMA(bars.Close, detrendPeriodFast);
            smaDetrendSlow = new SMA (bars.Close, detrendPeriodSlow);
            smaDetrend = new SMA (bars.Close, detrendPeriodBar);    
       
            if (enableDebugLogging)
            {
                WLLogger.Write("Detrend Indicator");
            }

            for (int i = detrendPeriodSlow; i < bars.Count; i++)
            {
       //         double value = smaDetrendFast[i] - smaDetrendSlow[i];
                smaDetrend[i] = smaDetrendFast[i] - smaDetrendSlow[i];

                if (enableDebugLogging)
                {
                    WLLogger.Write("Symbol: " + bars.Symbol +
                                   " dtrendFast: " + smaDetrendFast[i].ToString("#.00") +
                                   " detrendSlow: " + smaDetrendSlow[i].ToString("#.00") +
                                   " detrendValue: " + smaDetrend[i].ToString("#.00"));
                }
            }
        }

        public void PlotDetrend(UserStrategyBase strategy)
        {
            strategy.PlotIndicator(smaDetrend, WLColor.Blue, PlotStyle.Line, false, "Detrend");
            strategy.DrawHorzLine(0, WLColor.Black, 1, LineStyle.Solid, "Detrend");
        }
    }
}
