"""
Mean Reversion Strategy - Handelt bei Abweichungen vom Durchschnitt
"""

from typing import Dict, Optional, Tuple
from .base_strategy import BaseStrategy
from ..utils.indicators import Indicators


class MeanReversionStrategy(BaseStrategy):
    """
    Mean-Reversion-Strategie basierend auf Bollinger Bändern

    Kaufsignal: Preis berührt unteres Bollinger Band
    Verkaufssignal: Preis berührt oberes Bollinger Band
    """

    def __init__(
        self,
        symbol: str = "EURUSD",
        timeframe: str = "H1",
        bb_period: int = 20,
        bb_std_dev: float = 2.0,
        rsi_period: int = 14,
        rsi_oversold: float = 30.0,
        rsi_overbought: float = 70.0
    ):
        """
        Initialisiert die Mean Reversion Strategie

        Args:
            symbol: Handelssymbol
            timeframe: Zeitrahmen
            bb_period: Bollinger Band Periode
            bb_std_dev: Bollinger Band Standardabweichungen
            rsi_period: RSI Periode
            rsi_oversold: RSI Überverkauft-Level
            rsi_overbought: RSI Überkauft-Level
        """
        super().__init__("Mean Reversion", symbol, timeframe)

        self.bb_period = bb_period
        self.bb_std_dev = bb_std_dev
        self.rsi_period = rsi_period
        self.rsi_oversold = rsi_oversold
        self.rsi_overbought = rsi_overbought

    def analyze(self, market_data: Dict) -> Tuple[str, Optional[Dict]]:
        """
        Analysiert Marktdaten und generiert Trading-Signal

        Args:
            market_data: Dictionary mit 'close', 'high', 'low'

        Returns:
            Tuple (signal, params)
        """
        closes = market_data.get('close', [])

        if len(closes) < self.bb_period:
            return ('HOLD', None)

        current_price = closes[-1]

        # Berechne Bollinger Bänder
        upper_bb, middle_bb, lower_bb = Indicators.bollinger_bands(
            closes, self.bb_period, self.bb_std_dev
        )

        # Berechne RSI für Bestätigung
        rsi = Indicators.rsi(closes, self.rsi_period)

        # Berechne Prozentuale Position im Band
        bb_range = upper_bb - lower_bb
        if bb_range == 0:
            return ('HOLD', None)

        bb_position = (current_price - lower_bb) / bb_range * 100

        # Berechne ATR für Stop Loss
        highs = market_data.get('high', [])
        lows = market_data.get('low', [])
        atr = Indicators.atr(highs, lows, closes, 14)

        params = {
            'atr': atr,
            'stop_loss_multiplier': 1.5,
            'take_profit_ratio': 2.0,
            'middle_bb': middle_bb
        }

        # Signal-Logik
        if self.position is None:
            # Kaufsignal: Preis nahe unterem Band + RSI überverkauft
            if bb_position < 10 and rsi < self.rsi_oversold:
                self.logger.info(
                    f"BUY Signal: Preis ({current_price:.5f}) nahe unterem BB ({lower_bb:.5f}), "
                    f"RSI ({rsi:.2f}) < {self.rsi_oversold}"
                )
                return ('BUY', params)

            # Verkaufssignal: Preis nahe oberem Band + RSI überkauft
            elif bb_position > 90 and rsi > self.rsi_overbought:
                self.logger.info(
                    f"SELL Signal: Preis ({current_price:.5f}) nahe oberem BB ({upper_bb:.5f}), "
                    f"RSI ({rsi:.2f}) > {self.rsi_overbought}"
                )
                return ('SELL', params)

        else:
            # Exit-Logik: Zurück zum Mittleren Band
            if self.position == 'long':
                # Schließe Long wenn Preis das mittlere Band erreicht oder RSI überkauft
                if current_price >= middle_bb or rsi > self.rsi_overbought:
                    self.logger.info(
                        f"CLOSE Long: Preis ({current_price:.5f}) erreicht mittleres BB ({middle_bb:.5f}) "
                        f"oder RSI ({rsi:.2f}) überkauft"
                    )
                    return ('CLOSE', params)

            elif self.position == 'short':
                # Schließe Short wenn Preis das mittlere Band erreicht oder RSI überverkauft
                if current_price <= middle_bb or rsi < self.rsi_oversold:
                    self.logger.info(
                        f"CLOSE Short: Preis ({current_price:.5f}) erreicht mittleres BB ({middle_bb:.5f}) "
                        f"oder RSI ({rsi:.2f}) überverkauft"
                    )
                    return ('CLOSE', params)

        return ('HOLD', params)

    def get_parameters(self) -> Dict:
        """
        Gibt die Strategie-Parameter zurück
        """
        return {
            'strategy': 'Mean Reversion',
            'bb_period': self.bb_period,
            'bb_std_dev': self.bb_std_dev,
            'rsi_period': self.rsi_period,
            'rsi_oversold': self.rsi_oversold,
            'rsi_overbought': self.rsi_overbought,
            'description': 'Handelt bei extremen Abweichungen vom Durchschnitt'
        }
