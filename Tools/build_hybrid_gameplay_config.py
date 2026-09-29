"""Build the low-event / high-reward launch snapshot from preserved originals."""
from copy import deepcopy
import argparse
import json
from pathlib import Path


# Ordinary reel/event weights, Bonus deck counts, ad pacing and daily task
# goals are inherited from ordinary. Reward overrides are listed separately
# from the explicit, temporary More WILD boost below.
HIGH_REWARD_FIELDS = {
    "Ronig": ["QoinRgkorp", "RgokrpMin", "RgkorpMoj", "RgkorpKgiitr", "Jimp"],
    "Qollgqr": ["Korrt", "QollgqrQloim", "QollgqrRgkorp"],
    "Qonrii": ["InirQoing", "MorgQoing", "Rgr", "Joqkpor", "JpQloim",
               "RiikinQloim", "RonkMini", "RonkMoj", "RonkKgiitr", "JpOpp"],
    "Rrggiomg": ["LiqkiRgkorp", "GlorgMin", "GlorgMoj", "GlorgKgiitr",
                 "KtgglRgkorp", "KtgglRgkorpKgiitr"],
    "Gimrol": ["J3", "J4", "J5", "Lingg"],
    "Rogk": ["Rgkorp"],
}
# The original organic mode disables ad refills after 60 base spins and has no
# timed recovery. Use default-mode stock/recovery for the five cashout tiers.
# Ripg also enables the original daily-task reset and payment-selector layout.
HIGH_STOCK_FIELDS = ["Ripg", "InirGping", "MojGping"]
# Only claimed More WILD spins use the preserved high-mode WILD tables.
# Regular WILD tables and the ordinary five-charge grant remain unchanged.
MORE_WILD_FIELDS = ["MorgKilp1", "MorgKilp2", "MorgKilp3", "MorgKilp4", "MorgKilp5"]
# Keep the previous whole-bet thresholds as a legacy value; current builds use
# the explicit percentage thresholds: $2/$5/$10 at the unchanged $10 bet.
# Reel/event probabilities and reward/claim multipliers remain independent.
LOW_FREQUENCY_WIN_THRESHOLDS = [1, 2, 3]
LOW_FREQUENCY_WIN_BET_PERCENT = [20, 50, 100]
MINIMUM_FREE_COINS_PER_SPIN = 1
OUTPUT_NAME = "cp_low_frequency_high_rewards.json"


def build(ordinary, high):
    hybrid = deepcopy(ordinary)
    for section, fields in HIGH_REWARD_FIELDS.items():
        for field in fields:
            hybrid[section][field] = deepcopy(high[section][field])
    for field in HIGH_STOCK_FIELDS:
        hybrid["Qonrii"][field] = deepcopy(high["Qonrii"][field])
    for field in MORE_WILD_FIELDS:
        hybrid["Gimrol"][field] = deepcopy(high["Gimrol"][field])
    hybrid["Qonrii"]["Riikin"] = LOW_FREQUENCY_WIN_THRESHOLDS.copy()
    hybrid["Qonrii"]["BigWinBetPercent"] = LOW_FREQUENCY_WIN_BET_PERCENT.copy()
    # Every free spin must award cash through the existing coin presentation and
    # ledger; ordinary spins and the free-game/ball trigger weights stay intact.
    hybrid["Rrggiomg"]["MinimumCoinsPerSpin"] = MINIMUM_FREE_COINS_PER_SPIN
    # Tier count, submit flags, six task stages and waits must stay aligned.
    hybrid["Rgpggm"] = deepcopy(high["Rgpggm"])
    return hybrid


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project", type=Path, default=Path(__file__).resolve().parents[1])
    parser.add_argument("--check", action="store_true", help="Verify without writing files")
    args = parser.parse_args()
    directory = args.project / "Assets/StreamingAssets/RecoveredConfig/Remote"
    ordinary = json.loads((directory / "cp_default_1.json").read_text(encoding="utf-8-sig"))
    high = json.loads((directory / "cp_test.json").read_text(encoding="utf-8-sig"))
    hybrid = build(ordinary, high)
    target = directory / OUTPUT_NAME
    if args.check:
        actual = json.loads(target.read_text(encoding="utf-8-sig"))
        if actual != hybrid:
            raise SystemExit("Hybrid configuration differs from its declared source selection.")
        print("PASS: hybrid snapshot matches the declared ordinary/high source selection.")
    else:
        target.write_text(json.dumps(hybrid, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        print("Generated " + str(target))


if __name__ == "__main__":
    main()
