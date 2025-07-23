using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WealthLab.Core;
using WealthLab.Backtest;

namespace CompIdxOverUnderDriver
{
    public static class TradeContextBuilder
    {
        public static TradeContext Build(BarHistory bars, int idx, IList<Position> openPositions)
        {
            var context = new TradeContext();

            context.OverBoughtPositionExists = openPositions.Any(pos =>
                pos.EntrySignalName == "Buy @ OverBought");

            context.MidpointPositionExists = openPositions.Any(pos =>
                pos.EntrySignalName == "Buy @ Midpoint");

            context.OverSoldPositionExists = openPositions.Any(pos =>
                pos.EntrySignalName == "Buy @ OverSold");

            context.NoTradeViolation = !openPositions.Any(pos =>
                pos.EntrySignalName == "Violation");

            // Expand this as needed later

            return context;
        }
    }
}
