"""
Feature engineering for transaction pair matching.
Computes similarity and difference features between two transactions.
"""

import numpy as np
from thefuzz import fuzz
from datetime import datetime, date
from typing import Union


def _safe_date(val) -> Union[datetime, date, None]:
    """Parse a date value safely."""
    if val is None:
        return None
    if isinstance(val, (datetime, date)):
        return val
    if isinstance(val, str):
        for fmt in ("%Y-%m-%d", "%Y-%m-%dT%H:%M:%S", "%Y-%m-%d %H:%M:%S", "%m/%d/%Y"):
            try:
                return datetime.strptime(val, fmt)
            except ValueError:
                continue
    return None


def _safe_float(val) -> float:
    """Parse a numeric value safely."""
    if val is None:
        return 0.0
    try:
        return float(val)
    except (ValueError, TypeError):
        return 0.0


def _safe_str(val) -> str:
    """Return string or empty string."""
    if val is None:
        return ""
    return str(val)


def _jaro_winkler_similarity(s1: str, s2: str) -> float:
    """Jaro-Winkler similarity using thefuzz's token_sort_ratio."""
    if not s1 and not s2:
        return 0.5
    if not s1 or not s2:
        return 0.0
    # Use token_sort_ratio as a proxy — returns 0-100 scale
    score = fuzz.token_sort_ratio(s1, s2)
    return score / 100.0


def compute_features(tx1: dict, tx2: dict) -> dict[str, float]:
    """
    Compute a feature vector for a pair of transactions.

    Parameters
    ----------
    tx1 : dict
        First transaction with keys: amount, currency, transaction_date,
        reference, source, description
    tx2 : dict
        Second transaction with same keys

    Returns
    -------
    dict[str, float]
        Feature dictionary suitable for ML model input
    """
    # Safely extract values
    amt1 = _safe_float(tx1.get("amount"))
    amt2 = _safe_float(tx2.get("amount"))
    cur1 = _safe_str(tx1.get("currency"))
    cur2 = _safe_str(tx2.get("currency"))
    src1 = _safe_str(tx1.get("source"))
    src2 = _safe_str(tx2.get("source"))
    ref1 = _safe_str(tx1.get("reference"))
    ref2 = _safe_str(tx2.get("reference"))
    desc1 = _safe_str(tx1.get("description"))
    desc2 = _safe_str(tx2.get("description"))
    dt1 = _safe_date(tx1.get("transaction_date"))
    dt2 = _safe_date(tx2.get("transaction_date"))

    # Amount features
    features = {}
    features["amount_diff_abs"] = abs(amt1 - amt2)

    max_amt = max(amt1, amt2)
    if max_amt > 0:
        features["amount_ratio"] = min(amt1, amt2) / max_amt
    else:
        features["amount_ratio"] = 1.0 if amt1 == amt2 else 0.0

    # Date features
    if dt1 is not None and dt2 is not None:
        delta = abs((dt1 - dt2).days)
        features["date_diff_days"] = float(delta)
        features["same_day_of_week"] = 1.0 if dt1.weekday() == dt2.weekday() else 0.0
        features["same_month"] = (
            1.0
            if (dt1.year == dt2.year and dt1.month == dt2.month)
            else 0.0
        )
    else:
        features["date_diff_days"] = -1.0
        features["same_day_of_week"] = 0.0
        features["same_month"] = 0.0

    # Categorical features
    features["same_currency"] = 1.0 if cur1.lower() == cur2.lower() else 0.0
    features["same_source"] = 1.0 if src1.lower() == src2.lower() else 0.0

    # Reference similarity
    features["ref_similarity"] = _jaro_winkler_similarity(ref1, ref2)
    features["ref_exact_match"] = (
        1.0 if ref1.strip().lower() == ref2.strip().lower() else 0.0
    )

    # Description similarity
    features["description_similarity"] = _jaro_winkler_similarity(desc1, desc2)

    # Rounded amount match
    features["amount_rounded_match"] = (
        1.0 if round(amt1) == round(amt2) else 0.0
    )

    # Is reversal (opposite signs)
    features["is_reversal"] = (
        1.0 if (amt1 > 0 and amt2 < 0) or (amt1 < 0 and amt2 > 0) else 0.0
    )

    # Log ratio
    if max_amt > 0:
        ratio = min(amt1, amt2) / max_amt
        features["amount_log_ratio"] = float(np.log(ratio + 1e-10))
    else:
        features["amount_log_ratio"] = 0.0

    return features


FEATURE_NAMES = [
    "amount_diff_abs",
    "amount_ratio",
    "date_diff_days",
    "same_currency",
    "same_source",
    "ref_similarity",
    "description_similarity",
    "amount_rounded_match",
    "is_reversal",
    "same_day_of_week",
    "same_month",
    "amount_log_ratio",
    "ref_exact_match",
]


def feature_matrix(pairs: list[tuple[dict, dict]]) -> np.ndarray:
    """
    Convert a list of transaction pairs into a feature matrix.

    Parameters
    ----------
    pairs : list[tuple[dict, dict]]
        List of (tx1, tx2) pairs

    Returns
    -------
    np.ndarray
        Feature matrix of shape (n_pairs, n_features)
    """
    rows = []
    for tx1, tx2 in pairs:
        feat = compute_features(tx1, tx2)
        rows.append([feat[name] for name in FEATURE_NAMES])
    return np.array(rows, dtype=np.float64)
