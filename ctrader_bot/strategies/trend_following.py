"""
Trend Following Strategy - Folgt dem Markttrend mit Moving Averages
"""

from typing import Dict, Optional, Tuple
from .base_strategy import BaseStrategy
from ..utils.indicators import Indicators


class TrendFollowingStrategy(BaseStrategy):
    """
    Trend-Following-Strategie basierend auf Moving Average Crossover

    Kaufsignal: Wenn der schnelle MA den langsamen MA von unten kreuzt
    Verkaufssignal: Wenn der schnelle MA den langsamen MA von oben kreuzt
    """

    def __init__(
        self,
        symbol: str = "EURUSD",
        timeframe: str = "H1",
        fast_ma_period: int = 10,
        slow_ma_period: int = 30,
        use_ema: bool = True,
        trend_filter_period: int = 200
    ):
        """
        Initialisiert die Trend Following Strategie

        Args:
            symbol: Handelssymbol
            timeframe: Zeitrahmen
            fast_ma_period: Periode für schnellen Moving Average
            slow_ma_period: Periode für langsamen Moving Average
            use_ema: EMA verwenden (sonst SMA)
            trend_filter_period: Periode für Trend-Filter
        """
        super().__init__("Trend Following", symbol, timeframe)

        self.fast_ma_period = fast_ma_period
        self.slow_ma_period = slow_ma_period
        self.use_ema = use_ema
        self.trend_filter_period = trend_filter_period

        self.previous_fast_ma = 0.0
        self.previous_slow_ma = 0.0

    def analyze(self, market_data: Dict) -> Tuple[str, Optional[Dict]]:
        """
        Analysiert Marktdaten und generiert Trading-Signal

        Args:
            market_data: Dictionary mit 'close', 'high', 'low', 'volume'

        Returns:
            Tuple (signal, params)
        """
        closes = market_data.get('close', [])

        if len(closes) < self.trend_filter_period:
            return ('HOLD', None)

        # Berechne Moving Averages
        if self.use_ema:
            fast_ma = Indicators.ema(closes, self.fast_ma_period)
            slow_ma = Indicators.ema(closes, self.slow_ma_period)
            trend_ma = Indicators.ema(closes, self.trend_filter_period)
        else:
            fast_ma = Indicators.sma(closes, self.fast_ma_period)
            slow_ma = Indicators.sma(closes, self.slow_ma_period)
            trend_ma = Indicators.sma(closes, self.trend_filter_period)

        current_price = closes[-1]

        # Trend-Filter: Nur in Richtung des übergeordneten Trends handeln
        uptrend = current_price > trend_ma
        downtrend = current_price < trend_ma

        # Erkenne Crossover
        bullish_cross = (
            self.previous_fast_ma <= self.previous_slow_ma and
            fast_ma > slow_ma
        )

        bearish_cross = (
            self.previous_fast_ma >= self.previous_slow_ma and
            fast_ma < slow_ma
        )

        # Aktualisiere vorherige Werte
        self.previous_fast_ma = fast_ma
        self.previous_slow_ma = slow_ma

        # Berechne ATR für Stop Loss
        highs = market_data.get('high', [])
        lows = market_data.get('low', [])
        atr = Indicators.atr(highs, lows, closes, 14)

        params = {
            'atr': atr,
            'stop_loss_multiplier': 2.0,
            'take_profit_ratio': 2.5
        }

        # Generiere Signale
        if self.position is None:
            if bullish_cross and uptrend:
                self.logger.info(
                    f"BUY Signal: Fast MA ({fast_ma:.5f}) > Slow MA ({slow_ma:.5f}), "
                    f"Preis ({current_price:.5f}) > Trend MA ({trend_ma:.5f})"
                )
                return ('BUY', params)
            elif bearish_cross and downtrend:
                self.logger.info(
                    f"SELL Signal: Fast MA ({fast_ma:.5f}) < Slow MA ({slow_ma:.5f}), "
                    f"Preis ({current_price:.5f}) < Trend MA ({trend_ma:.5f})"
                )
                return ('SELL', params)
        else:
            # Position Management
            if self.position == 'long' and bearish_cross:
                self.logger.info("CLOSE Long Position: Bearish Crossover")
                return ('CLOSE', params)
            elif self.position == 'short' and bullish_cross:
                self.logger.info("CLOSE Short Position: Bullish Crossover")
                return ('CLOSE', params)

        return ('HOLD', params)

    def get_parameters(self) -> Dict:
        """
        Gibt die Strategie-Parameter zurück
        """
        return {
            'strategy': 'Trend Following',
            'fast_ma_period': self.fast_ma_period,
            'slow_ma_period': self.slow_ma_period,
            'use_ema': self.use_ema,
            'trend_filter_period': self.trend_filter_period,
            'description': 'Folgt Markttrends mit Moving Average Crossover'
        }
