"""
Basis-Klasse für alle Trading-Strategien
"""

from abc import ABC, abstractmethod
from typing import Dict, List, Optional, Tuple
from datetime import datetime
import logging


class BaseStrategy(ABC):
    """
    Abstrakte Basisklasse für alle Trading-Strategien.
    Alle Strategien müssen diese Klasse erweitern.
    """

    def __init__(self, name: str, symbol: str, timeframe: str = "H1"):
        """
        Initialisiert die Basis-Strategie

        Args:
            name: Name der Strategie
            symbol: Handelssymbol (z.B. EURUSD)
            timeframe: Zeitrahmen (M1, M5, M15, H1, H4, D1)
        """
        self.name = name
        self.symbol = symbol
        self.timeframe = timeframe
        self.logger = logging.getLogger(f"Strategy.{name}")
        self.position = None  # Aktuelle Position (None, 'long', 'short')
        self.entry_price = 0.0
        self.position_size = 0.0

    @abstractmethod
    def analyze(self, market_data: Dict) -> Tuple[str, Optional[Dict]]:
        """
        Analysiert Marktdaten und gibt ein Trading-Signal zurück

        Args:
            market_data: Dictionary mit Marktdaten (OHLCV)

        Returns:
            Tuple[signal, params]:
                signal: 'BUY', 'SELL', 'CLOSE', oder 'HOLD'
                params: Dictionary mit zusätzlichen Parameters (SL, TP, etc.)
        """
        pass

    @abstractmethod
    def get_parameters(self) -> Dict:
        """
        Gibt die Strategie-Parameter zurück

        Returns:
            Dictionary mit allen konfigurierbaren Parametern
        """
        pass

    def set_position(self, position_type: str, entry_price: float, size: float):
        """
        Setzt die aktuelle Position

        Args:
            position_type: 'long' oder 'short'
            entry_price: Einstiegspreis
            size: Positionsgröße
        """
        self.position = position_type
        self.entry_price = entry_price
        self.position_size = size
        self.logger.info(f"Position eröffnet: {position_type} @ {entry_price}, Size: {size}")

    def close_position(self, exit_price: float):
        """
        Schließt die aktuelle Position

        Args:
            exit_price: Ausstiegspreis
        """
        if self.position:
            profit = self._calculate_profit(exit_price)
            self.logger.info(
                f"Position geschlossen: {self.position} @ {exit_price}, "
                f"Profit: {profit:.2f}"
            )
            self.position = None
            self.entry_price = 0.0
            self.position_size = 0.0
            return profit
        return 0.0

    def _calculate_profit(self, current_price: float) -> float:
        """
        Berechnet den aktuellen Gewinn/Verlust

        Args:
            current_price: Aktueller Preis

        Returns:
            Gewinn/Verlust in Pips oder Prozent
        """
        if not self.position or self.entry_price == 0:
            return 0.0

        if self.position == 'long':
            return (current_price - self.entry_price) * self.position_size
        else:  # short
            return (self.entry_price - current_price) * self.position_size

    def get_status(self) -> Dict:
        """
        Gibt den aktuellen Status der Strategie zurück

        Returns:
            Dictionary mit Status-Informationen
        """
        return {
            'name': self.name,
            'symbol': self.symbol,
            'timeframe': self.timeframe,
            'position': self.position,
            'entry_price': self.entry_price,
            'position_size': self.position_size
        }

    def __str__(self):
        return f"{self.name} ({self.symbol}, {self.timeframe})"
