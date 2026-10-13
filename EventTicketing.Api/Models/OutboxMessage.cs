using System.ComponentModel.DataAnnotations;

namespace EventTicketing.Api.Models;

/// <summary>
/// A message saved in the same transaction as the change it announces; the outbox worker
/// publishes it to RabbitMQ afterwards, so a crash or broker outage can't lose it.
/// </summary>
public class OutboxMessage
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Type { get; set; } = string.Empty;

    // The JSON message body, stored as jsonb.
    [Required]
    public string Payload { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Exchange { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string RoutingKey { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    // Set once the broker has confirmed the message.
    public DateTime? ProcessedAt { get; set; }

    // Publish attempts so far; after its limit of failures the worker stops retrying (parks the message).
    public int Attempts { get; set; }

    // Earliest time of the next retry, pushed back further after each failure.
    public DateTime? NextAttemptAt { get; set; }

    [MaxLength(2000)]
    public string? LastError { get; set; }

    // The W3C traceparent of the request that saved the message, so publishing it continues that trace.
    [MaxLength(55)]
    public string? TraceParent { get; set; }
}
