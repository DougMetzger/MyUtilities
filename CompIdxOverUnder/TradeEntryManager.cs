using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WealthLab.Core;
using WealthLab.Backtest;
using WealthLab.MyIndicators;

namespace CompIdxOverUnderDriver
{
    public class TradeEntryManager
    {
        private readonly BarHistory bars;
        private readonly Customci customCI;
        private readonly bool[] volSpikeFlags;
        private readonly double overSoldLevel;
        private readonly double overBoughtLevel;
        private readonly bool hasOverSoldPosition; 
        private readonly bool hasOverBoughtPosition;
        private readonly bool hasMidpointPosition; 
        private readonly bool enableDebugLogging;
        private readonly Action<BarHistory, TransactionType, OrderType, double, double, string> placeTrade;

#pragma warning disable IDE0290
        public TradeEntryManager(
            BarHistory bars,
            Customci customCI,
            bool[] volSpikeFlags,
            double overSoldLevel,
            double overBoughtLevel,
            bool hasOverSoldPosition,
            bool hasOverBoughtPosition, 
            bool hasMidpointPosition,            
            bool enableDebugLogging,
            Action<BarHistory, TransactionType, OrderType, double, double, string> placeTrade)        
        {
            this.bars = bars;
            this.customCI = customCI;
            this.volSpikeFlags = volSpikeFlags;
            this.overSoldLevel = overSoldLevel;
            this.overBoughtLevel = overBoughtLevel;
            this.hasOverSoldPosition = hasOverSoldPosition;
            this.hasOverBoughtPosition = hasOverBoughtPosition;
            this.hasMidpointPosition = hasMidpointPosition; 
            this.enableDebugLogging = enableDebugLogging;
            this.placeTrade = placeTrade;
        }

        public void Evaluate(int index)
        {
            if (enableDebugLogging)
                WLLogger.Write($"TradeEntryManager: Evaluating index {index}");

            if (!volSpikeFlags[index])
                return;

            double midPoint = (overBoughtLevel + overSoldLevel) / 2.0;

            if (customCI.CrossesOver(overSoldLevel, index) &&
                !hasOverSoldPosition)
            {
                PlaceAndLogTrade("Buy @ OverSold", index);
                return;
            }

            if (customCI.CrossesOver(overBoughtLevel, index) &&
                !hasOverBoughtPosition)
            {
                PlaceAndLogTrade("Buy @ OverBought", index);
                return;
            }

            if (customCI.CrossesOver(midPoint, index) &&
                !hasMidpointPosition)
            {
                PlaceAndLogTrade("Buy @ MidPoint", index);
            }
        }

        private void PlaceAndLogTrade(string reason, int index)
        {
            placeTrade?.Invoke(bars, TransactionType.Buy, OrderType.Market, 0, 0, reason);

            if (enableDebugLogging)
                WLLogger.Write($"{reason} @ index {index}, symbol {bars.Symbol}");
        }       
    }
}
