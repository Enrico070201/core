"""
Grid Trading Strategy - Platziert Orders in einem Raster
"""

from typing import Dict, Optional, Tuple, List
from .base_strategy import BaseStrategy


class GridTradingStrategy(BaseStrategy):
    """
    Grid-Trading-Strategie

    Platziert Buy- und Sell-Orders in gleichmäßigen Abständen um einen Pivot-Preis
    Geeignet für seitwärts laufende Märkte
    """

    def __init__(
        self,
        symbol: str = "EURUSD",
        timeframe: str = "H1",
        grid_levels: int = 5,
        grid_spacing: float = 0.0010,  # 10 Pips
        take_profit_pips: float = 0.0015,  # 15 Pips
        max_positions: int = 3
    ):
        """
        Initialisiert die Grid Trading Strategie

        Args:
            symbol: Handelssymbol
            timeframe: Zeitrahmen
            grid_levels: Anzahl der Grid-Levels ober- und unterhalb
            grid_spacing: Abstand zwischen Grid-Levels
            take_profit_pips: Take Profit pro Grid-Trade
            max_positions: Maximale Anzahl gleichzeitiger Positionen
        """
        super().__init__("Grid Trading", symbol, timeframe)

        self.grid_levels = grid_levels
        self.grid_spacing = grid_spacing
        self.take_profit_pips = take_profit_pips
        self.max_positions = max_positions

        self.pivot_price = 0.0
        self.grid_buy_levels: List[float] = []
        self.grid_sell_levels: List[float] = []
        self.active_positions: List[Dict] = []

    def _initialize_grid(self, current_price: float):
        """
        Initialisiert das Grid um den aktuellen Preis

        Args:
            current_price: Aktueller Marktpreis
        """
        self.pivot_price = current_price
        self.grid_buy_levels = []
        self.grid_sell_levels = []

        # Erstelle Grid-Levels
        for i in range(1, self.grid_levels + 1):
            # Buy-Levels unterhalb des aktuellen Preises
            buy_level = current_price - (i * self.grid_spacing)
            self.grid_buy_levels.append(buy_level)

            # Sell-Levels oberhalb des aktuellen Preises
            sell_level = current_price + (i * self.grid_spacing)
            self.grid_sell_levels.append(sell_level)

        self.logger.info(
            f"Grid initialisiert um Preis {current_price:.5f}, "
            f"Buy Levels: {len(self.grid_buy_levels)}, Sell Levels: {len(self.grid_sell_levels)}"
        )

    def analyze(self, market_data: Dict) -> Tuple[str, Optional[Dict]]:
        """
        Analysiert Marktdaten und generiert Trading-Signal

        Args:
            market_data: Dictionary mit 'close', 'high', 'low'

        Returns:
            Tuple (signal, params)
        """
        closes = market_data.get('close', [])

        if len(closes) < 2:
            return ('HOLD', None)

        current_price = closes[-1]

        # Initialisiere Grid beim ersten Mal
        if self.pivot_price == 0.0:
            self._initialize_grid(current_price)
            return ('HOLD', None)

        # Prüfe ob wir zu weit vom Grid entfernt sind (Re-Initialize)
        distance_from_pivot = abs(current_price - self.pivot_price)
        max_distance = self.grid_spacing * self.grid_levels * 2

        if distance_from_pivot > max_distance:
            self.logger.info(f"Grid zu weit entfernt, re-initialisiere bei {current_price:.5f}")
            self._initialize_grid(current_price)
            return ('HOLD', None)

        # Prüfe maximale Positionen
        if len(self.active_positions) >= self.max_positions:
            return self._check_exit_conditions(current_price)

        # Prüfe ob Preis ein Grid-Level erreicht hat
        signal, params = self._check_grid_levels(current_price)

        if signal != 'HOLD':
            return (signal, params)

        # Prüfe Exit-Bedingungen für aktive Positionen
        return self._check_exit_conditions(current_price)

    def _check_grid_levels(self, current_price: float) -> Tuple[str, Optional[Dict]]:
        """
        Prüft ob der Preis ein Grid-Level erreicht hat

        Args:
            current_price: Aktueller Preis

        Returns:
            Tuple (signal, params)
        """
        # Prüfe Buy-Levels (Preis ist gefallen)
        for buy_level in self.grid_buy_levels:
            if abs(current_price - buy_level) < self.grid_spacing * 0.1:  # 10% Toleranz
                # Prüfe ob bereits eine Position auf diesem Level existiert
                if not self._has_position_at_level(buy_level):
                    params = {
                        'entry_price': buy_level,
                        'take_profit': buy_level + self.take_profit_pips,
                        'stop_loss': buy_level - (self.grid_spacing * 2),
                        'grid_level': buy_level
                    }
                    self.logger.info(f"BUY Signal bei Grid-Level: {buy_level:.5f}")
                    return ('BUY', params)

        # Prüfe Sell-Levels (Preis ist gestiegen)
        for sell_level in self.grid_sell_levels:
            if abs(current_price - sell_level) < self.grid_spacing * 0.1:
                if not self._has_position_at_level(sell_level):
                    params = {
                        'entry_price': sell_level,
                        'take_profit': sell_level - self.take_profit_pips,
                        'stop_loss': sell_level + (self.grid_spacing * 2),
                        'grid_level': sell_level
                    }
                    self.logger.info(f"SELL Signal bei Grid-Level: {sell_level:.5f}")
                    return ('SELL', params)

        return ('HOLD', None)

    def _check_exit_conditions(self, current_price: float) -> Tuple[str, Optional[Dict]]:
        """
        Prüft Exit-Bedingungen für aktive Positionen

        Args:
            current_price: Aktueller Preis

        Returns:
            Tuple (signal, params)
        """
        for pos in self.active_positions:
            if pos['type'] == 'long':
                # Take Profit erreicht
                if current_price >= pos['take_profit']:
                    self.logger.info(
                        f"CLOSE Long: Take Profit erreicht bei {current_price:.5f} "
                        f"(Entry: {pos['entry']:.5f})"
                    )
                    self.active_positions.remove(pos)
                    return ('CLOSE', None)

            elif pos['type'] == 'short':
                # Take Profit erreicht
                if current_price <= pos['take_profit']:
                    self.logger.info(
                        f"CLOSE Short: Take Profit erreicht bei {current_price:.5f} "
                        f"(Entry: {pos['entry']:.5f})"
                    )
                    self.active_positions.remove(pos)
                    return ('CLOSE', None)

        return ('HOLD', None)

    def _has_position_at_level(self, level: float) -> bool:
        """
        Prüft ob bereits eine Position auf diesem Level existiert

        Args:
            level: Grid-Level

        Returns:
            True wenn Position existiert
        """
        for pos in self.active_positions:
            if abs(pos['entry'] - level) < self.grid_spacing * 0.1:
                return True
        return False

    def set_position(self, position_type: str, entry_price: float, size: float):
        """
        Setzt eine neue Position und fügt sie zu aktiven Positionen hinzu

        Args:
            position_type: 'long' oder 'short'
            entry_price: Einstiegspreis
            size: Positionsgröße
        """
        super().set_position(position_type, entry_price, size)

        # Füge zur Liste aktiver Positionen hinzu
        position = {
            'type': position_type,
            'entry': entry_price,
            'size': size,
            'take_profit': entry_price + self.take_profit_pips if position_type == 'long'
                          else entry_price - self.take_profit_pips
        }
        self.active_positions.append(position)

    def get_parameters(self) -> Dict:
        """
        Gibt die Strategie-Parameter zurück
        """
        return {
            'strategy': 'Grid Trading',
            'grid_levels': self.grid_levels,
            'grid_spacing': self.grid_spacing,
            'take_profit_pips': self.take_profit_pips,
            'max_positions': self.max_positions,
            'active_positions': len(self.active_positions),
            'description': 'Platziert Orders in einem gleichmäßigen Raster'
        }
