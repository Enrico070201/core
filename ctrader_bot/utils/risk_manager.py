"""
Risk Management System für den Trading Bot
"""

import logging
from typing import Dict, Optional


class RiskManager:
    """
    Verwaltet das Risiko und die Positionsgrößen für Trades
    """

    def __init__(
        self,
        account_balance: float,
        max_risk_per_trade: float = 0.02,  # 2% pro Trade
        max_position_size: float = 0.1,     # 10% des Kontos
        max_daily_loss: float = 0.05,       # 5% maximaler Tagesverlust
        use_trailing_stop: bool = True
    ):
        """
        Initialisiert den Risk Manager

        Args:
            account_balance: Kontostand
            max_risk_per_trade: Maximales Risiko pro Trade (als Dezimalzahl)
            max_position_size: Maximale Positionsgröße (als Dezimalzahl)
            max_daily_loss: Maximaler Tagesverlust (als Dezimalzahl)
            use_trailing_stop: Trailing Stop verwenden
        """
        self.account_balance = account_balance
        self.initial_balance = account_balance
        self.max_risk_per_trade = max_risk_per_trade
        self.max_position_size = max_position_size
        self.max_daily_loss = max_daily_loss
        self.use_trailing_stop = use_trailing_stop

        self.daily_pnl = 0.0
        self.total_trades = 0
        self.winning_trades = 0
        self.losing_trades = 0

        self.logger = logging.getLogger("RiskManager")

    def calculate_position_size(
        self,
        entry_price: float,
        stop_loss: float,
        risk_percentage: Optional[float] = None
    ) -> float:
        """
        Berechnet die optimale Positionsgröße basierend auf Risiko

        Args:
            entry_price: Einstiegspreis
            stop_loss: Stop-Loss-Preis
            risk_percentage: Optionales spezifisches Risiko (überschreibt Standard)

        Returns:
            Positionsgröße (Anzahl Lots)
        """
        if risk_percentage is None:
            risk_percentage = self.max_risk_per_trade

        # Berechne Risiko in Kontowährung
        risk_amount = self.account_balance * risk_percentage

        # Berechne Distanz zum Stop Loss
        price_distance = abs(entry_price - stop_loss)

        if price_distance == 0:
            return 0.0

        # Berechne Positionsgröße
        position_size = risk_amount / price_distance

        # Limitiere auf maximale Positionsgröße
        max_size = self.account_balance * self.max_position_size / entry_price
        position_size = min(position_size, max_size)

        # Runde auf 2 Dezimalstellen (Standard-Lot-Größe)
        position_size = round(position_size, 2)

        self.logger.info(
            f"Position Size berechnet: {position_size} Lots "
            f"(Risk: {risk_amount:.2f}, Distance: {price_distance:.5f})"
        )

        return position_size

    def calculate_stop_loss(
        self,
        entry_price: float,
        direction: str,
        atr: float,
        multiplier: float = 2.0
    ) -> float:
        """
        Berechnet Stop Loss basierend auf ATR

        Args:
            entry_price: Einstiegspreis
            direction: 'long' oder 'short'
            atr: Average True Range Wert
            multiplier: ATR-Multiplikator

        Returns:
            Stop Loss Preis
        """
        stop_distance = atr * multiplier

        if direction == 'long':
            stop_loss = entry_price - stop_distance
        else:  # short
            stop_loss = entry_price + stop_distance

        return round(stop_loss, 5)

    def calculate_take_profit(
        self,
        entry_price: float,
        stop_loss: float,
        direction: str,
        risk_reward_ratio: float = 2.0
    ) -> float:
        """
        Berechnet Take Profit basierend auf Risk/Reward Ratio

        Args:
            entry_price: Einstiegspreis
            stop_loss: Stop Loss Preis
            direction: 'long' oder 'short'
            risk_reward_ratio: Risk/Reward Verhältnis

        Returns:
            Take Profit Preis
        """
        risk = abs(entry_price - stop_loss)
        reward = risk * risk_reward_ratio

        if direction == 'long':
            take_profit = entry_price + reward
        else:  # short
            take_profit = entry_price - reward

        return round(take_profit, 5)

    def can_trade(self) -> bool:
        """
        Prüft ob Trading erlaubt ist (Tagesverlust-Limit nicht erreicht)

        Returns:
            True wenn Trading erlaubt ist
        """
        max_loss = self.initial_balance * self.max_daily_loss

        if self.daily_pnl < -max_loss:
            self.logger.warning(
                f"Tagesverlust-Limit erreicht: {self.daily_pnl:.2f} / {-max_loss:.2f}"
            )
            return False

        return True

    def update_balance(self, pnl: float):
        """
        Aktualisiert Kontostand und Statistiken

        Args:
            pnl: Profit/Loss des Trades
        """
        self.account_balance += pnl
        self.daily_pnl += pnl
        self.total_trades += 1

        if pnl > 0:
            self.winning_trades += 1
        elif pnl < 0:
            self.losing_trades += 1

        self.logger.info(
            f"Balance aktualisiert: {self.account_balance:.2f} "
            f"(PnL: {pnl:+.2f}, Daily PnL: {self.daily_pnl:+.2f})"
        )

    def reset_daily_stats(self):
        """
        Setzt tägliche Statistiken zurück (am Tagesanfang aufrufen)
        """
        self.daily_pnl = 0.0
        self.initial_balance = self.account_balance
        self.logger.info("Tägliche Statistiken zurückgesetzt")

    def get_statistics(self) -> Dict:
        """
        Gibt aktuelle Statistiken zurück

        Returns:
            Dictionary mit Risk Management Statistiken
        """
        win_rate = 0.0
        if self.total_trades > 0:
            win_rate = (self.winning_trades / self.total_trades) * 100

        return {
            'account_balance': self.account_balance,
            'daily_pnl': self.daily_pnl,
            'total_trades': self.total_trades,
            'winning_trades': self.winning_trades,
            'losing_trades': self.losing_trades,
            'win_rate': win_rate,
            'max_risk_per_trade': self.max_risk_per_trade * 100,
            'max_position_size': self.max_position_size * 100
        }

    def update_trailing_stop(
        self,
        current_price: float,
        entry_price: float,
        current_stop: float,
        direction: str,
        trailing_distance: float
    ) -> float:
        """
        Aktualisiert Trailing Stop

        Args:
            current_price: Aktueller Preis
            entry_price: Einstiegspreis
            current_stop: Aktueller Stop Loss
            direction: 'long' oder 'short'
            trailing_distance: Abstand für Trailing Stop

        Returns:
            Neuer Stop Loss Preis
        """
        if not self.use_trailing_stop:
            return current_stop

        if direction == 'long':
            # Nur nach oben anpassen
            new_stop = current_price - trailing_distance
            if new_stop > current_stop and current_price > entry_price:
                self.logger.info(f"Trailing Stop aktualisiert: {current_stop:.5f} -> {new_stop:.5f}")
                return new_stop
        else:  # short
            # Nur nach unten anpassen
            new_stop = current_price + trailing_distance
            if new_stop < current_stop and current_price < entry_price:
                self.logger.info(f"Trailing Stop aktualisiert: {current_stop:.5f} -> {new_stop:.5f}")
                return new_stop

        return current_stop
