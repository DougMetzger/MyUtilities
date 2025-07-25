using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WealthLab.Backtest;
using WealthLab.Core;
using WealthLab.Indicators;
using WealthLab.MyIndicators;

namespace CompIdxOverUnderDriver
{
    public class TradeExitManager
    {
        private readonly BarHistory bars;
        private readonly bool enableDebugLogging;
        private readonly bool bearishDivergence;
        private readonly Customci customCI;
        private readonly double overBoughtLevel;
        private readonly double overSoldLevel;
        private readonly double sellAtStopLossPct;
        private readonly double sellAtProfitPct;
        //       private readonly bool issueStopLoss;
        //       private readonly bool takeProfits;
        private readonly Func<IEnumerable<Position>> getOpenPositions;
        private readonly Func<Position> getLastOpenPosition;
        private readonly Action<Position, OrderType, double, string> closePosition;

        private string message;
        bool stopLoss = false;
        bool takeProfit = false;
        int currentOpenPosIdx = 0;
        double sellAtStopLoss = 0;
        double sellAtProfit = 0;
        double highAmt = 0;
        double percent = 0;

        public TradeExitManager(
            BarHistory bars,
            bool enableDebugLogging,
            bool bearishDivergence,
            Customci customCI,
            double overSoldLevel,
            double overBoughtLevel,
            double sellAtStopLossPct,
            double sellAtProfitPct,
            //          bool issueStopLoss,
            //          bool takeProfits,
            Func<IEnumerable<Position>> getOpenPositions,
            Func<Position> getLastOpenPosition,
            Action<Position, OrderType, double, string> closePosition)

        {
            this.bars = bars;
            this.enableDebugLogging = enableDebugLogging;
            this.bearishDivergence = bearishDivergence;
            this.customCI = customCI;
            this.overSoldLevel = overSoldLevel;
            this.overBoughtLevel = overBoughtLevel;
            this.sellAtStopLossPct = sellAtStopLossPct;
            this.sellAtProfitPct = sellAtProfitPct;
            //          this.issueStopLoss = issueStopLoss;
            //          this.takeProfits = takeProfits;
            this.getOpenPositions = getOpenPositions;
            this.getLastOpenPosition = getLastOpenPosition;

            this.closePosition = closePosition;
        }

        public void CloseTrades(int index)
        {
            if (enableDebugLogging)
                WLLogger.Write($"TradeExitManager: CloseTrades Evaluation @ index {index}");

            var openPositions = getOpenPositions().ToList();
            if (!openPositions.Any())
            {
                return;
            }

            // 1) Bearish divergence
            if (this.bearishDivergence)
            {
                CloseAll("Sell Bearish Divergence", index, openPositions);
                return;
            }

            // 2) Cross under OverBought
            if (this.customCI.CrossesUnder(overBoughtLevel, index))
            {
                CloseAll("Sold @ OverBought", index, openPositions);
                return;
            }

            // 3) Cross under OverSold
            if (this.customCI.CrossesUnder(overSoldLevel, index))
            {
                CloseAll("Sold @ OverSold", index, openPositions);
                return;

            }

            // (4) Issue StopLoss
            percent = sellAtStopLossPct / 100.0;
            message = "Stop Loss " + (percent.ToString("P0"));
            CheckForStopLoss(bars, message, index, openPositions);

            // (5) Issue Take Profits
            percent = sellAtProfitPct / 100.0;
            message = "Take Profit " + (percent.ToString("P0"));
            CheckForProfitTaking(bars, message, index, openPositions);
        }

        private void CloseAll(string reason, int index, List<Position> openPositions)
        {
            foreach (var pos in openPositions)
            {
                this.closePosition(pos, OrderType.Market, 0, reason);
                if (enableDebugLogging)
                    WLLogger.Write($"Closed All Pos {reason}: {index + 1}");
            }

            if (enableDebugLogging)
                WLLogger.Write($"Total closed: {openPositions.Count} at index {index + 1}");
        }

        private void CheckForStopLoss(BarHistory bars, string reason, int index, List<Position> openPositions)
        {
            foreach (var pos in openPositions)
            {
                highAmt = pos.EntryPrice;
                currentOpenPosIdx = pos.EntryBar;

                while (currentOpenPosIdx <= (index))
                {
                    if (bars.Close[currentOpenPosIdx] > highAmt)
                    {
                        highAmt = bars.Close[currentOpenPosIdx];
                    }
                    currentOpenPosIdx++;
                }

                sellAtStopLoss = highAmt * (1 - (sellAtStopLossPct / 100));

                if (bars.Close[index] <= sellAtStopLoss)
                {
                    this.closePosition(pos, OrderType.Market, 0, reason);

                    if (enableDebugLogging == true)
                    {
                        WLLogger.Write("Closed because of Stop Loss");
                    }
                }
            }

        }
        private void CheckForProfitTaking(BarHistory bars, string reason, int index, List<Position> openPositions)
        {
            //
            //  If any position reaches its profit goal, close all open position and exit.
            //	
            sellAtProfit = (sellAtProfitPct / 100.0) + 1.0;

            foreach (var pos in openPositions)
            {
                if (bars.Close[index] >= (pos.EntryPrice * sellAtProfit))
                {
                    this.closePosition(pos, OrderType.Market, 0, reason);

                    if (enableDebugLogging == true)
                    {
                        WLLogger.Write("Closed because Profit Target Reached: " + (index + 1));
                    }

 //                   return;
                }
            }

        }

    }
}

