using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using WealthLab.Backtest;
using WealthLab.Core;
using WealthLab.MyIndicators;
using static CompIdxOverUnderDriver.CompIdxOUDriver;

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
        private readonly UserStrategyBase strategy;
        private readonly IDetrendIndicatorProvider detrendIndicatorManager;


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
            Action<BarHistory, TransactionType, OrderType, double, double, string> placeTrade,
            UserStrategyBase strategy,
           IDetrendIndicatorProvider detrendIndicatorManager)
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
            this.strategy = strategy;
            this.detrendIndicatorManager =  detrendIndicatorManager; 
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
                PlaceAndLogTrade("Buy @ OverSold", index, strategy);
                return;
            }

            if (customCI.CrossesOver(overBoughtLevel, index) &&
                !hasOverBoughtPosition)
            {
                PlaceAndLogTrade("Buy @ OverBought", index, strategy);
                return;
            }

            if (customCI.CrossesOver(midPoint, index) &&
                !hasMidpointPosition)
            {
                PlaceAndLogTrade("Buy @ MidPoint", index, strategy);
            }
        }

        private void PlaceAndLogTrade(string reason, int index, UserStrategyBase strategy)
        {

            int tempIndex = (index - 1);
  //          detrendManager = new DetrendIndicator.DetrendIndicatorManager(bars, 7);  // Provided 7 to fullfill constructor, but doesn't play a roll in processing.
            
            if (detrendIndicatorManager.IsDetrendNegative(index))
            {
                if (enableDebugLogging)
                {
                    WLLogger.Write($"TradeEntryManager: Detrend is *** NEGATIVE *** at index {index}, skipping trade.");                          
                  
                    strategy.DrawText("↑", tempIndex, 0, WLColor.Blue, 30, "Detrend Indicator");
                      

                    return;
                }
                else
                {
                    return;
                }
            }

            placeTrade?.Invoke(bars, TransactionType.Buy, OrderType.Market, 0, 0, reason);

            if (enableDebugLogging)
            {
                WLLogger.Write($"*********************************PlaceTrade:********************************* {reason} @ index {index}, symbol {bars.Symbol}");
            }
        }                                                                                      
         
    }
}
