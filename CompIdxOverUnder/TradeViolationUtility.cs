using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WealthLab.Backtest;
using WealthLab.Core;

namespace CompIdxOverUnderDriver
{
    public class TradeViolationUtil
    {
        public bool NoTradeViolation { get; private set; }
        public int? DaysSinceLastClose { get; private set; }
        public int? ExitBar { get; private set; }
        public string Message { get; private set; }

        public TradeViolationUtil(
            UserStrategyBase strategy,
            BarHistory bars,
            int index,
            int avoidTradingViolation,
            bool enableDebugLogging)
        {
            if (enableDebugLogging)
                WLLogger.Write("DetermineIfTradeViolation");

            if (strategy.LastOpenPosition != null)
            {
                Message = $"Symbol: {bars.Symbol} has open position(s) on bar {index} dated: {bars.DateTimes[index]:d}";
                NoTradeViolation = true;
                LogMessage(enableDebugLogging);
                return;
            }

            if (bars.Scale == HistoryScale.Weekly)
            {
                Message = $"Symbol: {bars.Symbol} Weekly Processing: No Trade Violation on bar {index} dated: {bars.DateTimes[index]:d}";
                NoTradeViolation = true;
                LogMessage(enableDebugLogging);
                return;
            }

            var lastClosed = strategy.GetPositions()
                .Where(p => !p.IsOpen)
                .OrderByDescending(p => p.ExitBar)
                .FirstOrDefault();

            if (lastClosed == null)
            {
                Message = $"Symbol: {bars.Symbol} No closed positions found on bar {index} dated: {bars.DateTimes[index]:d}";
                NoTradeViolation = true;
                LogMessage(enableDebugLogging);
                return;
            }

            ExitBar = lastClosed.ExitBar;
            DaysSinceLastClose = index - ExitBar;
            NoTradeViolation = DaysSinceLastClose > avoidTradingViolation;

            var status = NoTradeViolation ? "No Trade Violation" : "***** Trade Violation *****";
            Message = $"Symbol: {bars.Symbol} {status} on bar {index} dated: {bars.DateTimes[index]:d} — Days since last close = {DaysSinceLastClose}";
            LogMessage(enableDebugLogging);
        }

        private void LogMessage(bool enableDebugLogging)
        {
            if (enableDebugLogging)
                WLLogger.Write(Message);
        }
    }

}
