"""
FastAPI inference service for the recon-engine ML matcher.
Loads a trained XGBoost model and exposes prediction endpoints.
"""

import json
import os
from pathlib import Path
from typing import Optional

import joblib
import numpy as np
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, Field
from prometheus_fastapi_instrumentator import Instrumentator

from recon_ml.features import compute_features, FEATURE_NAMES

# ---------- Configuration ----------
MODEL_PATH = os.environ.get(
    "MODEL_PATH",
    str(Path(__file__).resolve().parent.parent / "models" / "recon-matcher-v1.pkl"),
)
META_PATH = os.environ.get(
    "META_PATH",
    str(Path(__file__).resolve().parent.parent / "models" / "recon-matcher-v1-meta.json"),
)

# ---------- Load model ----------
_model = None
_optimal_threshold = 0.5


def load_model():
    """Load the model and metadata from disk."""
    global _model, _optimal_threshold

    model_path = Path(MODEL_PATH)
    meta_path = Path(META_PATH)

    if not model_path.exists():
        raise RuntimeError(f"Model not found at {MODEL_PATH}")

    _model = joblib.load(str(model_path))
    print(f"Model loaded from {MODEL_PATH}")

    # Load metadata for optimal threshold
    if meta_path.exists():
        with open(str(meta_path)) as f:
            meta = json.load(f)
        _optimal_threshold = meta.get("optimal_threshold", 0.5)
        print(f"Optimal threshold: {_optimal_threshold}")
    else:
        _optimal_threshold = 0.5
        print(f"Metadata not found, using default threshold: {_optimal_threshold}")


# ---------- Pydantic models ----------


class Transaction(BaseModel):
    """A single financial transaction."""
    amount: float = Field(..., description="Transaction amount")
    currency: str = Field(..., description="Currency code (e.g. USD, EUR)")
    transaction_date: str = Field(
        ..., description="Transaction date (YYYY-MM-DD or ISO format)"
    )
    reference: Optional[str] = Field(None, description="Transaction reference")
    source: Optional[str] = Field(None, description="Source system name")
    description: Optional[str] = Field(None, description="Transaction description")


class PairInput(BaseModel):
    """A pair of transactions to evaluate."""
    tx1: Transaction
    tx2: Transaction


class BatchPredictRequest(BaseModel):
    """Batch prediction request with multiple pairs."""
    pairs: list[PairInput]


class PredictionResult(BaseModel):
    """Prediction result for a single pair."""
    match_probability: float = Field(
        ..., description="Probability of match (0-1)"
    )
    is_match: bool = Field(
        ..., description="Whether this pair is predicted to match"
    )
    confidence_score: float = Field(
        ..., description="Confidence score (same as match_probability)"
    )


class BatchPredictResponse(BaseModel):
    """Batch prediction response."""
    predictions: list[PredictionResult]


class HealthResponse(BaseModel):
    """Health check response."""
    status: str = "ok"
    model_loaded: bool
    threshold: float


# ---------- FastAPI app ----------
app = FastAPI(
    title="Recon-Engine ML Matcher",
    description="ML-based transaction matching service for financial reconciliation",
    version="1.0.0",
)


@app.on_event("startup")
async def startup_event():
    """Load model on application startup."""
    try:
        load_model()
    except RuntimeError as e:
        print(f"WARNING: {e}")
        print("Model will not be available until /reload endpoint is called.")


@app.get("/health", response_model=HealthResponse)
async def health():
    """Health check endpoint."""
    return HealthResponse(
        status="ok",
        model_loaded=_model is not None,
        threshold=_optimal_threshold,
    )


@app.post("/predict", response_model=BatchPredictResponse)
async def predict(request: BatchPredictRequest):
    """
    Predict match probabilities for one or more transaction pairs.

    Accepts a batch of pairs and returns match probabilities and predictions.
    """
    if _model is None:
        raise HTTPException(
            status_code=503, detail="Model not loaded. Call /reload first."
        )

    if not request.pairs:
        raise HTTPException(status_code=400, detail="No pairs provided.")

    results = []
    for pair in request.pairs:
        tx1 = pair.tx1.model_dump()
        tx2 = pair.tx2.model_dump()

        # Compute features
        feat = compute_features(tx1, tx2)
        feature_row = np.array([[feat[name] for name in FEATURE_NAMES]], dtype=np.float64)

        # Predict
        proba = float(_model.predict_proba(feature_row)[0, 1])
        is_match = proba >= _optimal_threshold

        results.append(
            PredictionResult(
                match_probability=round(proba, 6),
                is_match=is_match,
                confidence_score=round(proba, 6),
            )
        )

    return BatchPredictResponse(predictions=results)


@app.post("/reload")
async def reload_model():
    """Reload the model from disk (useful for model updates without restart)."""
    try:
        load_model()
        return {"status": "ok", "message": "Model reloaded successfully"}
    except RuntimeError as e:
        raise HTTPException(status_code=500, detail=str(e))


# ---------- Prometheus metrics ----------
Instrumentator().instrument(app).expose(app)


# ---------- Helper for standalone use ----------
def predict_single(tx1: dict, tx2: dict) -> PredictionResult:
    """Predict match for a single pair (standalone usage)."""
    if _model is None:
        load_model()
    feat = compute_features(tx1, tx2)
    feature_row = np.array([[feat[name] for name in FEATURE_NAMES]], dtype=np.float64)
    proba = float(_model.predict_proba(feature_row)[0, 1])
    is_match = proba >= _optimal_threshold
    return PredictionResult(
        match_probability=round(proba, 6),
        is_match=is_match,
        confidence_score=round(proba, 6),
    )
