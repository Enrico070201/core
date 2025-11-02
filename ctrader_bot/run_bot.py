#!/usr/bin/env python3
"""
Haupt-Runner für den cTrader Bot
"""

import sys
import time
from typing import Optional
from datetime import datetime

from bot import TradingBot
from config import (
    AVAILABLE_STRATEGIES,
    get_strategy_config,
    list_strategies,
    create_custom_config
)
from market_data_simulator import MarketDataSimulator, create_sample_data


def select_strategy() -> str:
    """
    Interaktive Strategieauswahl

    Returns:
        Name der gewählten Strategie
    """
    print("\n" + "="*60)
    print("  cTrader Bot - Strategieauswahl")
    print("="*60 + "\n")

    strategies = list(AVAILABLE_STRATEGIES.keys())

    for i, (key, strategy) in enumerate(AVAILABLE_STRATEGIES.items(), 1):
        print(f"{i}. {strategy['name']}")
        print(f"   {strategy['description']}")
        print(f"   Geeignet für: {strategy['best_for']}\n")

    while True:
        try:
            choice = input(f"Wähle eine Strategie (1-{len(strategies)}) oder 'q' zum Beenden: ")

            if choice.lower() == 'q':
                print("Programm beendet.")
                sys.exit(0)

            choice_num = int(choice)
            if 1 <= choice_num <= len(strategies):
                selected = strategies[choice_num - 1]
                print(f"\n✓ Strategie gewählt: {AVAILABLE_STRATEGIES[selected]['name']}\n")
                return selected
            else:
                print(f"Bitte wähle eine Zahl zwischen 1 und {len(strategies)}")

        except ValueError:
            print("Ungültige Eingabe. Bitte gib eine Zahl ein.")
        except KeyboardInterrupt:
            print("\n\nProgramm beendet.")
            sys.exit(0)


def configure_bot() -> dict:
    """
    Konfiguriert Bot-Parameter

    Returns:
        Konfigurationsdictionary
    """
    print("\n" + "="*60)
    print("  Bot-Konfiguration")
    print("="*60 + "\n")

    # Symbol
    symbol = input("Handelssymbol (Standard: EURUSD): ").strip().upper()
    if not symbol:
        symbol = "EURUSD"

    # Timeframe
    print("\nVerfügbare Zeitrahmen: M1, M5, M15, H1, H4, D1")
    timeframe = input("Zeitrahmen (Standard: H1): ").strip().upper()
    if not timeframe:
        timeframe = "H1"

    # Initial Balance
    try:
        balance_input = input("Startkapital (Standard: 10000): ").strip()
        initial_balance = float(balance_input) if balance_input else 10000.0
    except ValueError:
        initial_balance = 10000.0

    # Risk per Trade
    try:
        risk_input = input("Risiko pro Trade in % (Standard: 2): ").strip()
        risk_per_trade = float(risk_input) / 100 if risk_input else 0.02
    except ValueError:
        risk_per_trade = 0.02

    config = {
        'symbol': symbol,
        'timeframe': timeframe,
        'initial_balance': initial_balance,
        'max_risk_per_trade': risk_per_trade
    }

    print("\n✓ Konfiguration abgeschlossen\n")
    return config


def run_demo_mode(bot: TradingBot, duration: int = 100):
    """
    Führt Bot im Demo-Modus mit simulierten Daten

    Args:
        bot: TradingBot-Instanz
        duration: Anzahl der zu simulierenden Candles
    """
    print("\n" + "="*60)
    print("  Demo-Modus - Simuliertes Trading")
    print("="*60 + "\n")

    # Erstelle Market Data Simulator
    simulator = create_sample_data(
        symbol=bot.symbol,
        bars=50,  # Initialisiere mit historischen Daten
        market_type='mixed'
    )

    print(f"Startpreis: {simulator.current_price:.5f}")
    print(f"Strategie: {bot.strategy.name}")
    print(f"Startkapital: {bot.risk_manager.account_balance:.2f}\n")
    print("Starte Trading...\n")

    bot.is_running = True
    candle_count = 0

    try:
        for i in range(duration):
            # Generiere nächste Candle
            candle = simulator.generate_next_candle()
            market_data = simulator.get_market_data()

            # Analysiere Markt
            trade = bot.analyze_market(market_data)

            candle_count += 1
            current_price = market_data['current_price']

            # Zeige Updates
            if trade:
                print(f"[{candle['timestamp'].strftime('%Y-%m-%d %H:%M')}] "
                      f"Preis: {current_price:.5f} | "
                      f"Action: {trade['action']} | "
                      f"Balance: {bot.risk_manager.account_balance:.2f}")

            # Status-Update alle 10 Candles
            elif candle_count % 10 == 0:
                position_status = f"Position: {bot.strategy.position}" if bot.strategy.position else "Keine Position"
                print(f"[{candle['timestamp'].strftime('%Y-%m-%d %H:%M')}] "
                      f"Preis: {current_price:.5f} | {position_status}")

            # Simuliere Zeitverzögerung
            time.sleep(0.1)  # 100ms pro Candle

    except KeyboardInterrupt:
        print("\n\nDemo-Modus gestoppt durch Benutzer")

    finally:
        bot.is_running = False

        # Zeige Zusammenfassung
        print("\n" + "="*60)
        print("  Trading-Zusammenfassung")
        print("="*60 + "\n")

        stats = bot.risk_manager.get_statistics()

        print(f"Startkapital:      {bot.config.get('initial_balance', 10000):.2f}")
        print(f"Endkapital:        {stats['account_balance']:.2f}")
        print(f"Gewinn/Verlust:    {stats['account_balance'] - bot.config.get('initial_balance', 10000):+.2f}")
        print(f"Trades gesamt:     {stats['total_trades']}")
        print(f"Gewinn-Trades:     {stats['winning_trades']}")
        print(f"Verlust-Trades:    {stats['losing_trades']}")
        print(f"Win Rate:          {stats['win_rate']:.2f}%")
        print(f"\nCandles verarbeitet: {candle_count}")

        # Speichere Status
        bot.save_state()
        print(f"\n✓ Status gespeichert in bot_state.json")


def run_live_mode(bot: TradingBot):
    """
    Führt Bot im Live-Modus (mit echten Daten - hier als Platzhalter)

    Args:
        bot: TradingBot-Instanz
    """
    print("\n" + "="*60)
    print("  Live-Modus")
    print("="*60 + "\n")

    print("HINWEIS: Live-Trading benötigt Verbindung zur cTrader API.")
    print("Diese Implementierung zeigt die Struktur. Für echtes Live-Trading")
    print("muss eine Verbindung zu cTrader Open API implementiert werden.\n")

    print("Verwende stattdessen den Demo-Modus für Simulation.")
    print("\nProgramm beendet.")


def main():
    """
    Hauptfunktion
    """
    print("\n╔════════════════════════════════════════════════════════════╗")
    print("║         cTrader Bot - Automatisches Trading System        ║")
    print("╚════════════════════════════════════════════════════════════╝")

    # Strategieauswahl
    strategy_name = select_strategy()

    # Bot-Konfiguration
    config = configure_bot()

    # Erstelle vollständige Konfiguration
    full_config = create_custom_config(
        strategy_name=strategy_name,
        **config
    )

    # Initialisiere Bot
    print("\nInitialisiere Bot...")
    bot = TradingBot(
        strategy_name=strategy_name,
        symbol=config['symbol'],
        timeframe=config['timeframe'],
        initial_balance=config['initial_balance'],
        config=full_config
    )

    print(f"✓ Bot erfolgreich initialisiert\n")

    # Modus-Auswahl
    print("\n" + "="*60)
    print("  Modus-Auswahl")
    print("="*60 + "\n")
    print("1. Demo-Modus (Simulierte Daten)")
    print("2. Live-Modus (Echte Marktdaten - benötigt API)")
    print("3. Beenden\n")

    while True:
        try:
            mode = input("Wähle Modus (1-3): ").strip()

            if mode == '1':
                duration = input("\nAnzahl der zu simulierenden Candles (Standard: 100): ").strip()
                duration = int(duration) if duration else 100
                run_demo_mode(bot, duration)
                break

            elif mode == '2':
                run_live_mode(bot)
                break

            elif mode == '3':
                print("Programm beendet.")
                break

            else:
                print("Ungültige Auswahl. Bitte wähle 1, 2 oder 3.")

        except ValueError:
            print("Ungültige Eingabe.")
        except KeyboardInterrupt:
            print("\n\nProgramm beendet.")
            break


if __name__ == "__main__":
    try:
        main()
    except Exception as e:
        print(f"\nFehler: {e}")
        import traceback
        traceback.print_exc()
        sys.exit(1)
