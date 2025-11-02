#!/usr/bin/env python3
"""
Quick-Start Demo - Schnelles Testen aller Strategien
"""

from bot import TradingBot
from config import AVAILABLE_STRATEGIES, create_custom_config
from market_data_simulator import create_sample_data


def test_strategy(strategy_name: str, market_type: str = 'mixed', duration: int = 100):
    """
    Testet eine einzelne Strategie

    Args:
        strategy_name: Name der Strategie
        market_type: Art des Marktes ('trending_up', 'trending_down', 'ranging', 'volatile', 'mixed')
        duration: Anzahl der Candles
    """
    print(f"\n{'='*70}")
    print(f"  Teste {AVAILABLE_STRATEGIES[strategy_name]['name']}")
    print(f"  Markttyp: {market_type}")
    print(f"{'='*70}\n")

    # Erstelle Konfiguration
    config = create_custom_config(
        strategy_name=strategy_name,
        symbol="EURUSD",
        timeframe="H1",
        initial_balance=10000.0
    )

    # Initialisiere Bot
    bot = TradingBot(
        strategy_name=strategy_name,
        symbol="EURUSD",
        timeframe="H1",
        initial_balance=10000.0,
        config=config
    )

    # Erstelle Marktdaten
    simulator = create_sample_data(
        symbol="EURUSD",
        bars=50,
        market_type=market_type
    )

    # Laufe durch Simulation
    trades_executed = 0

    for i in range(duration):
        # Generiere nächste Candle
        candle = simulator.generate_next_candle()
        market_data = simulator.get_market_data()

        # Analysiere Markt
        trade = bot.analyze_market(market_data)

        if trade:
            trades_executed += 1
            print(f"  [{i+1}/{duration}] {trade['action']:5s} @ {market_data['current_price']:.5f}")

    # Zeige Ergebnisse
    stats = bot.risk_manager.get_statistics()

    initial = 10000.0
    final = stats['account_balance']
    profit = final - initial
    profit_pct = (profit / initial) * 100

    print(f"\n  Ergebnisse:")
    print(f"  ├─ Startkapital:   {initial:,.2f}")
    print(f"  ├─ Endkapital:     {final:,.2f}")
    print(f"  ├─ Gewinn/Verlust: {profit:+,.2f} ({profit_pct:+.2f}%)")
    print(f"  ├─ Trades:         {stats['total_trades']}")
    print(f"  ├─ Gewonnen:       {stats['winning_trades']}")
    print(f"  ├─ Verloren:       {stats['losing_trades']}")
    print(f"  └─ Win Rate:       {stats['win_rate']:.2f}%\n")

    return {
        'strategy': strategy_name,
        'market_type': market_type,
        'profit': profit,
        'profit_pct': profit_pct,
        'trades': stats['total_trades'],
        'win_rate': stats['win_rate']
    }


def test_all_strategies():
    """
    Testet alle Strategien in verschiedenen Marktbedingungen
    """
    print("\n╔════════════════════════════════════════════════════════════════════╗")
    print("║      cTrader Bot - Strategievergleich                             ║")
    print("╚════════════════════════════════════════════════════════════════════╝")

    market_types = ['trending_up', 'ranging', 'volatile', 'mixed']
    results = []

    for market_type in market_types:
        print(f"\n\n{'#'*70}")
        print(f"  MARKTTYP: {market_type.upper()}")
        print(f"{'#'*70}")

        for strategy_name in AVAILABLE_STRATEGIES.keys():
            result = test_strategy(strategy_name, market_type, duration=50)
            results.append(result)

    # Zeige Zusammenfassung
    print("\n\n" + "="*70)
    print("  GESAMTZUSAMMENFASSUNG")
    print("="*70 + "\n")

    # Gruppiere nach Strategie
    strategy_totals = {}
    for result in results:
        strategy = result['strategy']
        if strategy not in strategy_totals:
            strategy_totals[strategy] = {
                'profit': 0,
                'trades': 0,
                'wins': 0,
                'markets': 0
            }

        strategy_totals[strategy]['profit'] += result['profit']
        strategy_totals[strategy]['trades'] += result['trades']
        strategy_totals[strategy]['markets'] += 1

    # Zeige Ergebnisse
    sorted_strategies = sorted(
        strategy_totals.items(),
        key=lambda x: x[1]['profit'],
        reverse=True
    )

    print(f"{'Strategie':<25} {'Gesamt P/L':<15} {'Trades':<10} {'Avg P/L':<15}")
    print("-" * 70)

    for strategy, totals in sorted_strategies:
        avg_profit = totals['profit'] / totals['markets']
        print(
            f"{AVAILABLE_STRATEGIES[strategy]['name']:<25} "
            f"{totals['profit']:+,.2f}€{'':<7} "
            f"{totals['trades']:<10} "
            f"{avg_profit:+,.2f}€"
        )

    print("\n" + "="*70)


def quick_demo():
    """
    Schnelle Demo einer einzelnen Strategie
    """
    print("\n╔════════════════════════════════════════════════════════════════════╗")
    print("║      cTrader Bot - Quick Demo                                     ║")
    print("╚════════════════════════════════════════════════════════════════════╝")

    # Teste RSI-Strategie (meist vielseitig)
    test_strategy('rsi', market_type='mixed', duration=100)


if __name__ == "__main__":
    import sys

    if len(sys.argv) > 1:
        command = sys.argv[1].lower()

        if command == 'all':
            # Teste alle Strategien
            test_all_strategies()

        elif command in AVAILABLE_STRATEGIES:
            # Teste spezifische Strategie
            market = sys.argv[2] if len(sys.argv) > 2 else 'mixed'
            test_strategy(command, market, duration=100)

        else:
            print(f"\nUnbekannter Befehl: {command}")
            print("\nVerwendung:")
            print("  python demo_quick_start.py          - Quick Demo (RSI Strategie)")
            print("  python demo_quick_start.py all      - Teste alle Strategien")
            print(f"  python demo_quick_start.py <strategy> [market] - Teste spezifische Strategie")
            print(f"\nVerfügbare Strategien: {', '.join(AVAILABLE_STRATEGIES.keys())}")
            print("Verfügbare Märkte: trending_up, trending_down, ranging, volatile, mixed")

    else:
        # Standard: Quick Demo
        quick_demo()
