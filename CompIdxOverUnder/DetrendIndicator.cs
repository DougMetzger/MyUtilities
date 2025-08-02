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
    internal class DetrendIndicator
    {
        public class DetrendIndicatorManager
        {

            public SMA smaDetrendShort { get; private set; }
            public SMA smaDetrendLong { get; private set; }
            public TimeSeries smaDetrendSpread { get; private set; }

            public int numDetrendShort = 1;
            public int numDetrendSpread = 1;


            public DetrendIndicatorManager(BarHistory bars, int numDetrendLong)
            {
                smaDetrendShort = new SMA(bars.Close, numDetrendShort);
                smaDetrendLong = new SMA(bars.Close, numDetrendLong);

                smaDetrendSpread = smaDetrendShort - smaDetrendLong;
            }

            public bool IsDetrendNegative(int idx)
            {
                return smaDetrendSpread[idx] < 0; 
            }   

            public void PlotDetrendIndicator(UserStrategyBase strategy)
            {
                strategy.PlotTimeSeries(smaDetrendSpread,"Detrend Indicator", "Detrend Indicator", WLColor.Blue, PlotStyle.Line, false);
                strategy.DrawHorzLine(0, WLColor.Black, 1, LineStyle.Solid, "Detrend Indicator");

                strategy.DrawHeaderText("Detrend Indicator", WLColor.Coral, 17, "Detrend Indicator", true);
            }
        }
    }
}
