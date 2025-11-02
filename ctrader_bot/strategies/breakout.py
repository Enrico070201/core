"""
Breakout Strategy - Handelt bei Ausbrüchen aus Handelsspannen
"""

from typing import Dict, Optional, Tuple, List
from .base_strategy import BaseStrategy
from ..utils.indicators import Indicators


class BreakoutStrategy(BaseStrategy):
    """
    Breakout-Strategie basierend auf Unterstützungs- und Widerstandsniveaus

    Kaufsignal: Preis bricht über Widerstand mit hohem Volumen
    Verkaufssignal: Preis bricht unter Unterstützung mit hohem Volumen
    """

    def __init__(
        self,
        symbol: str = "EURUSD",
        timeframe: str = "H1",
        lookback_period: int = 20,
        breakout_threshold: float = 0.0001,  # Breakout muss mindestens X sein
        volume_multiplier: float = 1.5,      # Volumen muss X-fach über Durchschnitt sein
        consolidation_threshold: float = 0.5  # Wie eng muss die Range sein (%)
    ):
        """
        Initialisiert die Breakout Strategie

        Args:
            symbol: Handelssymbol
            timeframe: Zeitrahmen
            lookback_period: Periode für Hoch/Tief-Berechnung
            breakout_threshold: Minimaler Breakout-Abstand
            volume_multiplier: Minimaler Volumen-Multiplikator
            consolidation_threshold: Schwelle für Range-Erkennung
        """
        super().__init__("Breakout", symbol, timeframe)

        self.lookback_period = lookback_period
        self.breakout_threshold = breakout_threshold
        self.volume_multiplier = volume_multiplier
        self.consolidation_threshold = consolidation_threshold

        self.resistance_level = 0.0
        self.support_level = 0.0

    def analyze(self, market_data: Dict) -> Tuple[str, Optional[Dict]]:
        """
        Analysiert Marktdaten und generiert Trading-Signal

        Args:
            market_data: Dictionary mit 'close', 'high', 'low', 'volume'

        Returns:
            Tuple (signal, params)
        """
        closes = market_data.get('close', [])
        highs = market_data.get('high', [])
        lows = market_data.get('low', [])
        volumes = market_data.get('volume', [])

        if len(closes) < self.lookback_period:
            return ('HOLD', None)

        current_price = closes[-1]
        current_volume = volumes[-1] if volumes else 0

        # Berechne Unterstützung und Widerstand
        self.support_level = min(lows[-self.lookback_period:])
        self.resistance_level = max(highs[-self.lookback_period:])

        # Berechne durchschnittliches Volumen
        avg_volume = sum(volumes[-self.lookback_period:]) / self.lookback_period if volumes else 1

        # Prüfe ob wir in einer Konsolidierung sind
        price_range = self.resistance_level - self.support_level
        mid_price = (self.resistance_level + self.support_level) / 2

        if mid_price == 0:
            return ('HOLD', None)

        range_percentage = (price_range / mid_price) * 100
        is_consolidating = range_percentage < self.consolidation_threshold

        # Berechne ATR für Stop Loss
        atr = Indicators.atr(highs, lows, closes, 14)

        # Prüfe Volumen-Bestätigung
        high_volume = current_volume > (avg_volume * self.volume_multiplier)

        params = {
            'atr': atr,
            'stop_loss_multiplier': 1.5,
            'take_profit_ratio': 3.0,
            'support': self.support_level,
            'resistance': self.resistance_level
        }

        # Signal-Logik
        if self.position is None:
            # Breakout nach oben
            breakout_up = current_price > (self.resistance_level + self.breakout_threshold)

            # Breakout nach unten
            breakout_down = current_price < (self.support_level - self.breakout_threshold)

            if breakout_up and high_volume:
                self.logger.info(
                    f"BUY Signal: Breakout über Widerstand ({self.resistance_level:.5f}), "
                    f"Preis: {current_price:.5f}, Volumen: {current_volume:.0f} (Avg: {avg_volume:.0f})"
                )
                return ('BUY', params)

            elif breakout_down and high_volume:
                self.logger.info(
                    f"SELL Signal: Breakout unter Unterstützung ({self.support_level:.5f}), "
                    f"Preis: {current_price:.5f}, Volumen: {current_volume:.0f} (Avg: {avg_volume:.0f})"
                )
                return ('SELL', params)

        else:
            # Exit-Logik: Zurück in die Range oder Stop Loss
            if self.position == 'long':
                # Schließe wenn Preis zurück unter Resistance fällt
                if current_price < self.resistance_level:
                    self.logger.info(
                        f"CLOSE Long: Preis ({current_price:.5f}) zurück unter Widerstand ({self.resistance_level:.5f})"
                    )
                    return ('CLOSE', params)

            elif self.position == 'short':
                # Schließe wenn Preis zurück über Support steigt
                if current_price > self.support_level:
                    self.logger.info(
                        f"CLOSE Short: Preis ({current_price:.5f}) zurück über Unterstützung ({self.support_level:.5f})"
                    )
                    return ('CLOSE', params)

        return ('HOLD', params)

    def get_parameters(self) -> Dict:
        """
        Gibt die Strategie-Parameter zurück
        """
        return {
            'strategy': 'Breakout',
            'lookback_period': self.lookback_period,
            'breakout_threshold': self.breakout_threshold,
            'volume_multiplier': self.volume_multiplier,
            'consolidation_threshold': self.consolidation_threshold,
            'description': 'Handelt bei Ausbrüchen aus Konsolidierungsphasen'
        }
