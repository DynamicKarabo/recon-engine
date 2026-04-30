using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReconciliationEngine.Application.Interfaces;
using ReconciliationEngine.Domain.Entities;
using ReconciliationEngine.Infrastructure.Configuration;

namespace ReconciliationEngine.Infrastructure.Services;

public class MLServiceClient : IMLServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MLServiceClient> _logger;
    private readonly MLServiceOptions _options;

    public MLServiceClient(
        HttpClient httpClient,
        IOptions<MLServiceOptions> options,
        ILogger<MLServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<decimal?> GetMatchScoreAsync(
        Transaction tx1,
        Transaction tx2,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new MLMatchRequest
            {
                Pairs = new[]
                {
                    new MLPair
                    {
                        Tx1 = MapTransaction(tx1),
                        Tx2 = MapTransaction(tx2)
                    }
                }
            };

            var response = await _httpClient.PostAsJsonAsync("/predict", request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "ML service returned {StatusCode} for pair ({Id1}, {Id2})",
                    response.StatusCode, tx1.Id, tx2.Id);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<MLBatchResponse>(cancellationToken: cancellationToken);
            return result?.Predictions?.FirstOrDefault()?.Score;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "ML service request failed for pair ({Id1}, {Id2})", tx1.Id, tx2.Id);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "ML service request timed out for pair ({Id1}, {Id2})", tx1.Id, tx2.Id);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize ML service response for pair ({Id1}, {Id2})", tx1.Id, tx2.Id);
            return null;
        }
    }

    public async Task<Dictionary<Guid, decimal>> GetBatchMatchScoresAsync(
        Transaction source,
        IEnumerable<Transaction> candidates,
        CancellationToken cancellationToken = default)
    {
        var candidateList = candidates.ToList();
        var scores = new Dictionary<Guid, decimal>();

        foreach (var candidate in candidateList)
        {
            var score = await GetMatchScoreAsync(source, candidate, cancellationToken);
            if (score.HasValue)
            {
                scores[candidate.Id] = score.Value;
            }
        }

        return scores;
    }

    private static MLTransaction MapTransaction(Transaction tx)
    {
        return new MLTransaction
        {
            Id = tx.Id.ToString(),
            Source = tx.Source,
            ExternalId = tx.ExternalId,
            Amount = tx.Amount,
            Currency = tx.Currency,
            TransactionDate = tx.TransactionDate,
            Description = tx.Description,
            Reference = tx.Reference,
            AccountId = tx.AccountId
        };
    }

    private class MLMatchRequest
    {
        public required MLPair[] Pairs { get; set; }
    }

    private class MLPair
    {
        public required MLTransaction Tx1 { get; set; }
        public required MLTransaction Tx2 { get; set; }
    }

    private class MLBatchResponse
    {
        public required MLPredictionResult[] Predictions { get; set; }
    }

    private class MLPredictionResult
    {
        [System.Text.Json.Serialization.JsonPropertyName("match_probability")]
        public decimal Score { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("is_match")]
        public bool IsMatch { get; set; }
    }

    private class MLTransaction
    {
        public string Id { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string ExternalId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public string? Description { get; set; }
        public string? Reference { get; set; }
        public string? AccountId { get; set; }
    }
}
