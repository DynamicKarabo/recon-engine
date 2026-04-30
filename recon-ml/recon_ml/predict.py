"""
Standalone prediction script for testing the recon-engine ML matcher.
Usage:
    python -m recon_ml.predict --tx1-amount 100 --tx1-reference "INV-001" \\
        --tx2-amount 100 --tx2-reference "INV-001"
"""

import argparse
import sys
from pathlib import Path

# Ensure recon_ml package is importable
sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from recon_ml.inference import predict_single


def main():
    parser = argparse.ArgumentParser(
        description="Predict transaction match probability"
    )
    # Transaction 1
    parser.add_argument("--tx1-amount", type=float, default=0.0)
    parser.add_argument("--tx1-currency", type=str, default="USD")
    parser.add_argument("--tx1-date", type=str, default="2024-01-15")
    parser.add_argument("--tx1-reference", type=str, default="")
    parser.add_argument("--tx1-source", type=str, default="BankFeedA")
    parser.add_argument("--tx1-description", type=str, default="")

    # Transaction 2
    parser.add_argument("--tx2-amount", type=float, default=0.0)
    parser.add_argument("--tx2-currency", type=str, default="USD")
    parser.add_argument("--tx2-date", type=str, default="2024-01-15")
    parser.add_argument("--tx2-reference", type=str, default="")
    parser.add_argument("--tx2-source", type=str, default="ERP")
    parser.add_argument("--tx2-description", type=str, default="")

    args = parser.parse_args()

    tx1 = {
        "amount": args.tx1_amount,
        "currency": args.tx1_currency,
        "transaction_date": args.tx1_date,
        "reference": args.tx1_reference if args.tx1_reference else None,
        "source": args.tx1_source,
        "description": args.tx1_description if args.tx1_description else None,
    }
    tx2 = {
        "amount": args.tx2_amount,
        "currency": args.tx2_currency,
        "transaction_date": args.tx2_date,
        "reference": args.tx2_reference if args.tx2_reference else None,
        "source": args.tx2_source,
        "description": args.tx2_description if args.tx2_description else None,
    }

    result = predict_single(tx1, tx2)

    print("=" * 50)
    print("RECON-ENGINE ML MATCHER — PREDICTION")
    print("=" * 50)
    print(f"  Transaction 1: {tx1['reference'] or 'N/A'} | {tx1['amount']} {tx1['currency']}")
    print(f"  Transaction 2: {tx2['reference'] or 'N/A'} | {tx2['amount']} {tx2['currency']}")
    print(f"  Match Probability: {result.match_probability:.4f}")
    print(f"  Is Match:         {result.is_match}")
    print(f"  Confidence Score: {result.confidence_score:.4f}")
    print("=" * 50)


if __name__ == "__main__":
    main()
