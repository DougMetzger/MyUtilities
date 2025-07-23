using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using WealthLab.Backtest;
using WealthLab.Core;
using WealthLab.Indicators;
using WealthLab.MyIndicators;

namespace CompIdxOverUnderDriver
{
    using WealthLab.Core;
    using WealthLab.Backtest;

    public static class DivergenceHelper
    {
     public static string DivergenceIdentifier(
                                            UserStrategyBase strategy,
                                            BarHistory bars,
                                            int index,
                                            IndicatorBase indicator,
                                            double reversal,
                                            string indicatorPaneTag,
                                            bool enableDebugLogging)
        {
            if (enableDebugLogging)
                WLLogger.Write("GraphDivergences");

            var ptc = new PeakTroughCalculator(indicator, reversal, PeakTroughReversalType.Point);

            if (ptc.PeakTroughs.Count < 4)
                return null;

            int startIndex = ptc.PeakTroughs[3].DetectedAtIndex;
            if (index < startIndex)
                return null;

            PeakTrough pt1, pt2;

            if (ptc.Divergence(index, bars.High, out pt1, out pt2) == DivergenceType.Bearish)
            {
                strategy.DrawBarAnnotation(TextShape.ArrowDown, index, true, WLColor.Gold, 36);
                strategy.DrawLine(pt1.XIndex, pt1.YValue, pt2.XIndex, pt2.YValue, WLColor.Red, 4, default, indicatorPaneTag);
                strategy.DrawLine(pt1.XIndex, bars.High[pt1.XIndex], pt2.XIndex, bars.High[pt2.XIndex], WLColor.Red, 4, default, "Price");
                return "E"; // ✅ Notify driver: bearish divergence occurred
            }

            if (ptc.Divergence(index, bars.Low, out pt1, out pt2) == DivergenceType.Bullish)
            {
                strategy.DrawBarAnnotation(TextShape.ArrowUp, index, false, WLColor.Gold, 36);
                strategy.DrawLine(pt1.XIndex, pt1.YValue, pt2.XIndex, pt2.YValue, WLColor.Green, 4, default, indicatorPaneTag);
                strategy.DrawLine(pt1.XIndex, bars.Low[pt1.XIndex], pt2.XIndex, bars.Low[pt2.XIndex], WLColor.Green, 4, default, "Price");
                return "U"; 
            }

            return null; // No divergence
        }

    }
}
