"""
RSI Strategy - Handelt basierend auf RSI-Indikator
"""

from typing import Dict, Optional, Tuple
from .base_strategy import BaseStrategy
from ..utils.indicators import Indicators


class RSIStrategy(BaseStrategy):
    """
    RSI-basierte Trading-Strategie

    Kaufsignal: RSI unter Überverkauft-Level und steigend
    Verkaufssignal: RSI über Überkauft-Level und fallend
    """

    def __init__(
        self,
        symbol: str = "EURUSD",
        timeframe: str = "H1",
        rsi_period: int = 14,
        oversold_level: float = 30.0,
        overbought_level: float = 70.0,
        extreme_oversold: float = 20.0,
        extreme_overbought: float = 80.0,
        use_divergence: bool = True
    ):
        """
        Initialisiert die RSI Strategie

        Args:
            symbol: Handelssymbol
            timeframe: Zeitrahmen
            rsi_period: RSI Periode
            oversold_level: Überverkauft-Level
            overbought_level: Überkauft-Level
            extreme_oversold: Extrem überverkauft (stärkeres Signal)
            extreme_overbought: Extrem überkauft (stärkeres Signal)
            use_divergence: Divergenz-Erkennung verwenden
        """
        super().__init__("RSI Strategy", symbol, timeframe)

        self.rsi_period = rsi_period
        self.oversold_level = oversold_level
        self.overbought_level = overbought_level
        self.extreme_oversold = extreme_oversold
        self.extreme_overbought = extreme_overbought
        self.use_divergence = use_divergence

        self.previous_rsi = 50.0
        self.rsi_history = []

    def analyze(self, market_data: Dict) -> Tuple[str, Optional[Dict]]:
        """
        Analysiert Marktdaten und generiert Trading-Signal

        Args:
            market_data: Dictionary mit 'close', 'high', 'low'

        Returns:
            Tuple (signal, params)
        """
        closes = market_data.get('close', [])

        if len(closes) < self.rsi_period + 1:
            return ('HOLD', None)

        current_price = closes[-1]

        # Berechne RSI
        rsi = Indicators.rsi(closes, self.rsi_period)

        # Speichere RSI in History für Divergenz-Analyse
        self.rsi_history.append(rsi)
        if len(self.rsi_history) > 50:
            self.rsi_history.pop(0)

        # Berechne RSI-Momentum (steigend/fallend)
        rsi_rising = rsi > self.previous_rsi
        rsi_falling = rsi < self.previous_rsi

        # Berechne MACD für zusätzliche Bestätigung
        macd_line, signal_line, histogram = Indicators.macd(closes)
        macd_bullish = histogram > 0

        # Berechne ATR für Stop Loss
        highs = market_data.get('high', [])
        lows = market_data.get('low', [])
        atr = Indicators.atr(highs, lows, closes, 14)

        params = {
            'atr': atr,
            'stop_loss_multiplier': 2.0,
            'take_profit_ratio': 2.5,
            'rsi': rsi
        }

        # Divergenz-Erkennung (optional)
        bullish_divergence = False
        bearish_divergence = False

        if self.use_divergence and len(closes) >= 20 and len(self.rsi_history) >= 20:
            bullish_divergence = self._detect_bullish_divergence(closes, self.rsi_history)
            bearish_divergence = self._detect_bearish_divergence(closes, self.rsi_history)

        # Signal-Logik
        if self.position is None:
            # Starkes Kaufsignal: RSI extrem überverkauft und steigend
            if rsi <= self.extreme_oversold and rsi_rising:
                self.logger.info(
                    f"BUY Signal (STRONG): RSI {rsi:.2f} extrem überverkauft und steigend"
                )
                return ('BUY', params)

            # Normales Kaufsignal: RSI überverkauft und steigend + MACD bullish
            elif rsi <= self.oversold_level and rsi_rising and macd_bullish:
                self.logger.info(
                    f"BUY Signal: RSI {rsi:.2f} überverkauft, steigend, MACD bullish"
                )
                return ('BUY', params)

            # Kaufsignal durch bullish Divergenz
            elif bullish_divergence and rsi < 50:
                self.logger.info(
                    f"BUY Signal (DIVERGENCE): Bullish Divergenz erkannt, RSI {rsi:.2f}"
                )
                return ('BUY', params)

            # Starkes Verkaufssignal: RSI extrem überkauft und fallend
            elif rsi >= self.extreme_overbought and rsi_falling:
                self.logger.info(
                    f"SELL Signal (STRONG): RSI {rsi:.2f} extrem überkauft und fallend"
                )
                return ('SELL', params)

            # Normales Verkaufssignal: RSI überkauft und fallend + MACD bearish
            elif rsi >= self.overbought_level and rsi_falling and not macd_bullish:
                self.logger.info(
                    f"SELL Signal: RSI {rsi:.2f} überkauft, fallend, MACD bearish"
                )
                return ('SELL', params)

            # Verkaufssignal durch bearish Divergenz
            elif bearish_divergence and rsi > 50:
                self.logger.info(
                    f"SELL Signal (DIVERGENCE): Bearish Divergenz erkannt, RSI {rsi:.2f}"
                )
                return ('SELL', params)

        else:
            # Exit-Logik
            if self.position == 'long':
                # Schließe Long wenn RSI überkauft oder fallende Divergenz
                if rsi >= self.overbought_level or bearish_divergence:
                    self.logger.info(
                        f"CLOSE Long: RSI {rsi:.2f} überkauft oder bearish divergence"
                    )
                    return ('CLOSE', params)

                # Trailing Exit: RSI fällt unter 50 (Momentum verloren)
                elif rsi < 50 and rsi_falling:
                    self.logger.info(
                        f"CLOSE Long: RSI {rsi:.2f} unter 50 und fallend (Momentum verloren)"
                    )
                    return ('CLOSE', params)

            elif self.position == 'short':
                # Schließe Short wenn RSI überverkauft oder steigende Divergenz
                if rsi <= self.oversold_level or bullish_divergence:
                    self.logger.info(
                        f"CLOSE Short: RSI {rsi:.2f} überverkauft oder bullish divergence"
                    )
                    return ('CLOSE', params)

                # Trailing Exit: RSI steigt über 50 (Momentum verloren)
                elif rsi > 50 and rsi_rising:
                    self.logger.info(
                        f"CLOSE Short: RSI {rsi:.2f} über 50 und steigend (Momentum verloren)"
                    )
                    return ('CLOSE', params)

        # Aktualisiere vorherigen RSI
        self.previous_rsi = rsi

        return ('HOLD', params)

    def _detect_bullish_divergence(self, prices: list, rsi_values: list) -> bool:
        """
        Erkennt bullish Divergenz (Preis macht tieferes Tief, RSI macht höheres Tief)

        Args:
            prices: Preisliste
            rsi_values: RSI-Werte-Liste

        Returns:
            True wenn bullish Divergenz erkannt
        """
        if len(prices) < 20 or len(rsi_values) < 20:
            return False

        # Finde die letzten beiden Tiefs
        recent_prices = prices[-20:]
        recent_rsi = rsi_values[-20:]

        # Vereinfachte Divergenz-Erkennung
        price_low_1 = min(recent_prices[:10])
        price_low_2 = min(recent_prices[10:])

        rsi_low_1 = min(recent_rsi[:10])
        rsi_low_2 = min(recent_rsi[10:])

        # Bullish Divergenz: Preis macht tieferes Tief, aber RSI macht höheres Tief
        return price_low_2 < price_low_1 and rsi_low_2 > rsi_low_1

    def _detect_bearish_divergence(self, prices: list, rsi_values: list) -> bool:
        """
        Erkennt bearish Divergenz (Preis macht höheres Hoch, RSI macht tieferes Hoch)

        Args:
            prices: Preisliste
            rsi_values: RSI-Werte-Liste

        Returns:
            True wenn bearish Divergenz erkannt
        """
        if len(prices) < 20 or len(rsi_values) < 20:
            return False

        recent_prices = prices[-20:]
        recent_rsi = rsi_values[-20:]

        price_high_1 = max(recent_prices[:10])
        price_high_2 = max(recent_prices[10:])

        rsi_high_1 = max(recent_rsi[:10])
        rsi_high_2 = max(recent_rsi[10:])

        # Bearish Divergenz: Preis macht höheres Hoch, aber RSI macht tieferes Hoch
        return price_high_2 > price_high_1 and rsi_high_2 < rsi_high_1

    def get_parameters(self) -> Dict:
        """
        Gibt die Strategie-Parameter zurück
        """
        return {
            'strategy': 'RSI Strategy',
            'rsi_period': self.rsi_period,
            'oversold_level': self.oversold_level,
            'overbought_level': self.overbought_level,
            'extreme_oversold': self.extreme_oversold,
            'extreme_overbought': self.extreme_overbought,
            'use_divergence': self.use_divergence,
            'description': 'Handelt basierend auf RSI-Signalen und Divergenzen'
        }
