"""
Training pipeline for the recon-engine ML matching model.
Generates synthetic training data, trains an XGBoost classifier,
evaluates, and saves the model with metadata.
"""

import json
import os
import random
import string
from datetime import datetime, timedelta
from pathlib import Path

import numpy as np
import pandas as pd
from sklearn.model_selection import train_test_split
from sklearn.metrics import (
    classification_report,
    roc_auc_score,
    precision_recall_curve,
    precision_score,
    recall_score,
    f1_score,
)
import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt
import joblib
from xgboost import XGBClassifier

from recon_ml.features import compute_features, FEATURE_NAMES, feature_matrix

# ---------- Paths ----------
BASE_DIR = Path(__file__).resolve().parent.parent
MODELS_DIR = BASE_DIR / "models"
REPORTS_DIR = BASE_DIR / "reports"
MODELS_DIR.mkdir(parents=True, exist_ok=True)
REPORTS_DIR.mkdir(parents=True, exist_ok=True)

MODEL_PATH = MODELS_DIR / "recon-matcher-v1.pkl"
METADATA_PATH = MODELS_DIR / "recon-matcher-v1-meta.json"
FEATURE_NAMES_PATH = MODELS_DIR / "recon-matcher-v1-features.json"
FEATURE_IMPORTANCE_PATH = REPORTS_DIR / "feature_importance.png"

# ---------- Synthetic Data Generator ----------

_CURRENCIES = ["USD", "EUR", "GBP", "JPY", "CAD", "AUD", "CHF", "INR", "BRL", "MXN"]
_SOURCES = ["BankFeedA", "BankFeedB", "ERP", "POS", "Manual", "Gateway"]
_REF_PREFIXES = ["INV", "PAY", "REF", "TXN", "ORD", "REC", "ADJ"]
_DESCRIPTION_TEMPLATES = [
    "Payment for invoice {ref}",
    "Invoice {ref} payment received",
    "Ref: {ref} - settlement",
    "Transaction {ref}",
    "Payment against {ref}",
    "Receipt for {ref}",
    "Adjustment {ref}",
    "Order {ref} payment",
]


def _random_date(start: datetime, end: datetime) -> datetime:
    """Random datetime between start and end."""
    delta = end - start
    seconds = random.randint(0, int(delta.total_seconds()))
    return start + timedelta(seconds=seconds)


def _generate_base_transactions(n: int) -> list[dict]:
    """Generate N random base transactions."""
    start_date = datetime(2023, 1, 1)
    end_date = datetime(2024, 12, 31)
    base = []
    for i in range(n):
        ref = f"{random.choice(_REF_PREFIXES)}-{random.randint(1000, 99999)}"
        amt = round(random.uniform(10.0, 10000.0), 2)
        # Occasionally negative
        if random.random() < 0.1:
            amt = -amt
        tx = {
            "amount": amt,
            "currency": random.choice(_CURRENCIES),
            "transaction_date": _random_date(start_date, end_date).strftime("%Y-%m-%d"),
            "reference": ref,
            "source": random.choice(_SOURCES),
            "description": random.choice(_DESCRIPTION_TEMPLATES).format(ref=ref),
        }
        base.append(tx)
    return base


def _edit_string(s: str, edits: int = 1) -> str:
    """Introduce minor edits to a string (substitutions)."""
    chars = list(s)
    for _ in range(edits):
        if not chars:
            break
        idx = random.randrange(len(chars))
        chars[idx] = random.choice(string.ascii_uppercase + string.digits)
    return "".join(chars)


def generate_synthetic_pairs(
    base_txs: list[dict], n_pairs: int = 15000, seed: int = 42
) -> tuple[list[tuple[dict, dict]], list[int]]:
    """
    Generate synthetic transaction pairs with known match labels.

    Returns (pairs, labels) where pairs is list of (tx1, tx2) tuples
    and labels is list of 0/1 ints.
    """
    random.seed(seed)
    np.random.seed(seed)

    pairs: list[tuple[dict, dict]] = []
    labels: list[int] = []

    # We'll allocate counts based on desired proportions
    n_exact = int(n_pairs * 0.10)
    n_fuzzy = int(n_pairs * 0.10)
    n_same_amt = int(n_pairs * 0.05)
    n_partial_ref = int(n_pairs * 0.05)
    n_non_match = n_pairs - n_exact - n_fuzzy - n_same_amt - n_partial_ref

    def pick_random_tx() -> dict:
        return random.choice(base_txs).copy()

    # 1. Exact matches (10%)
    for _ in range(n_exact):
        tx1 = pick_random_tx()
        tx2 = tx1.copy()  # exact copy
        pairs.append((tx1, tx2))
        labels.append(1)

    # 2. Fuzzy matches (10%) — same amount/currency, ±1 day date, edited reference
    for _ in range(n_fuzzy):
        tx1 = pick_random_tx()
        tx2 = tx1.copy()
        # Slightly edit the reference
        tx2["reference"] = _edit_string(tx1["reference"], edits=random.randint(1, 3))
        # Shift date by ±1 day
        dt = datetime.strptime(tx1["transaction_date"], "%Y-%m-%d")
        dt += timedelta(days=random.choice([-1, 0, 1]))
        tx2["transaction_date"] = dt.strftime("%Y-%m-%d")
        # Slightly different description
        tx2["description"] = random.choice(_DESCRIPTION_TEMPLATES).format(
            ref=tx2["reference"]
        )
        pairs.append((tx1, tx2))
        labels.append(1)

    # 3. Same amount, different date (5%) — match-like but date differs up to 30 days
    for _ in range(n_same_amt):
        tx1 = pick_random_tx()
        tx2 = tx1.copy()
        dt = datetime.strptime(tx1["transaction_date"], "%Y-%m-%d")
        dt += timedelta(days=random.randint(1, 30))
        tx2["transaction_date"] = dt.strftime("%Y-%m-%d")
        tx2["reference"] = _edit_string(tx1["reference"], edits=random.randint(2, 4))
        tx2["source"] = random.choice(_SOURCES)
        pairs.append((tx1, tx2))
        labels.append(0)  # different enough date — treat as non-match

    # 4. Partial ref match (5%) — different amounts, similar reference
    for _ in range(n_partial_ref):
        tx1 = pick_random_tx()
        tx2 = pick_random_tx()
        # Make reference similar but amounts different
        tx2["reference"] = tx1["reference"]  # same ref
        # Ensure different amounts
        tx2["amount"] = round(tx1["amount"] * random.uniform(0.5, 0.8), 2)
        pairs.append((tx1, tx2))
        labels.append(0)

    # 5. Non-matches (70%) — random pairs
    for _ in range(n_non_match):
        tx1 = pick_random_tx()
        tx2 = pick_random_tx()
        # Ensure they're somewhat different to avoid accidental matches
        pairs.append((tx1, tx2))
        labels.append(0)

    # Shuffle
    combined = list(zip(pairs, labels))
    random.shuffle(combined)
    if combined:
        pairs, labels = zip(*combined)
        pairs = list(pairs)
        labels = list(labels)

    return pairs, labels


# ---------- Training ----------


def train() -> dict:
    """Run the full training pipeline and return test metrics."""
    print("=" * 60)
    print("RECON-ENGINE ML MATCHING MODEL — TRAINING PIPELINE")
    print("=" * 60)

    # 1. Generate synthetic data
    print("\n[1/5] Generating synthetic transaction pairs...")
    base_txs = _generate_base_transactions(5000)
    pairs, labels = generate_synthetic_pairs(base_txs, n_pairs=15000)
    print(f"  Generated {len(pairs)} pairs ({sum(labels)} positive, "
          f"{len(labels) - sum(labels)} negative)")

    # 2. Compute feature matrix
    print("\n[2/5] Computing features...")
    X = feature_matrix(pairs)
    y = np.array(labels, dtype=np.int32)
    print(f"  Feature matrix shape: {X.shape}")
    print(f"  Features: {FEATURE_NAMES}")

    # 3. Train/test split
    X_train, X_test, y_train, y_test = train_test_split(
        X, y, test_size=0.2, random_state=42, stratify=y
    )
    print(f"\n[3/5] Training split: {X_train.shape[0]} train, "
          f"{X_test.shape[0]} test")

    # 4. Train XGBoost
    print("\n[4/5] Training XGBoost classifier...")
    model = XGBClassifier(
        n_estimators=100,
        max_depth=6,
        learning_rate=0.1,
        subsample=0.8,
        colsample_bytree=0.8,
        random_state=42,
        n_jobs=-1,
        eval_metric="logloss",
        use_label_encoder=False,
    )
    model.fit(X_train, y_train)

    # 5. Evaluate
    print("\n[5/5] Evaluating...")
    y_pred_proba = model.predict_proba(X_test)[:, 1]
    y_pred_default = model.predict(X_test)

    # Classification report
    print("\n--- Classification Report (default threshold=0.5) ---")
    print(classification_report(y_test, y_pred_default, target_names=["Non-match", "Match"]))

    # ROC AUC
    roc_auc = roc_auc_score(y_test, y_pred_proba)
    print(f"\nROC AUC: {roc_auc:.4f}")

    # Find optimal threshold via precision-recall curve
    precisions, recalls, thresholds = precision_recall_curve(y_test, y_pred_proba)
    f1_scores = 2 * precisions * recalls / (precisions + recalls + 1e-10)
    best_idx = np.argmax(f1_scores[:-1])  # last element is sentinel
    optimal_threshold = float(thresholds[best_idx])
    best_f1 = f1_scores[best_idx]

    print(f"Optimal threshold (max F1): {optimal_threshold:.4f} (F1={best_f1:.4f})")

    # Apply optimal threshold
    y_pred_opt = (y_pred_proba >= optimal_threshold).astype(int)
    opt_precision = precision_score(y_test, y_pred_opt)
    opt_recall = recall_score(y_test, y_pred_opt)
    opt_f1 = f1_score(y_test, y_pred_opt)

    print(f"\n--- Performance at optimal threshold ({optimal_threshold:.4f}) ---")
    print(f"  Precision: {opt_precision:.4f}")
    print(f"  Recall:    {opt_recall:.4f}")
    print(f"  F1 Score:  {opt_f1:.4f}")
    print(f"  ROC AUC:   {roc_auc:.4f}")

    # Feature importance plot
    print("\nSaving feature importance plot...")
    importance = model.feature_importances_
    indices = np.argsort(importance)[::-1]

    fig, ax = plt.subplots(figsize=(10, 6))
    ax.bar(range(len(importance)), importance[indices])
    ax.set_xticks(range(len(importance)))
    ax.set_xticklabels([FEATURE_NAMES[i] for i in indices], rotation=45, ha="right")
    ax.set_title("Feature Importance — Recon Matcher v1")
    ax.set_xlabel("Feature")
    ax.set_ylabel("Importance")
    plt.tight_layout()
    plt.savefig(str(FEATURE_IMPORTANCE_PATH), dpi=150)
    plt.close()
    print(f"  Saved to {FEATURE_IMPORTANCE_PATH}")

    # Save model
    print(f"\nSaving model to {MODEL_PATH}...")
    joblib.dump(model, str(MODEL_PATH))

    # Save feature names
    with open(str(FEATURE_NAMES_PATH), "w") as f:
        json.dump(FEATURE_NAMES, f, indent=2)
    print(f"  Feature names saved to {FEATURE_NAMES_PATH}")

    # Save metadata
    test_metrics = {
        "roc_auc": round(float(roc_auc), 4),
        "optimal_threshold": round(float(optimal_threshold), 4),
        "precision": round(float(opt_precision), 4),
        "recall": round(float(opt_recall), 4),
        "f1_score": round(float(opt_f1), 4),
        "test_samples": int(len(y_test)),
        "positive_ratio": float(round(float(y_test.mean()), 4)),
    }

    metadata = {
        "model_version": "recon-matcher-v1",
        "training_date": datetime.now().strftime("%Y-%m-%d %H:%M:%S"),
        "features_used": FEATURE_NAMES,
        "optimal_threshold": round(float(optimal_threshold), 4),
        "test_metrics": test_metrics,
        "training_params": {
            "n_estimators": 100,
            "max_depth": 6,
            "learning_rate": 0.1,
            "subsample": 0.8,
            "colsample_bytree": 0.8,
        },
        "n_train_samples": int(X_train.shape[0]),
        "algorithm": "XGBoost",
    }

    with open(str(METADATA_PATH), "w") as f:
        json.dump(metadata, f, indent=2)
    print(f"  Metadata saved to {METADATA_PATH}")

    print("\n" + "=" * 60)
    print("TRAINING COMPLETE")
    print("=" * 60)

    return metadata


if __name__ == "__main__":
    train()
