namespace EventTicketing.Api.Messaging;

/// <summary>Exchange and queue names; integration tests give each test host its own.</summary>
public class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string Exchange { get; set; } = "ticketing";

    public string DeadLetterExchange { get; set; } = "ticketing.dlx";

    public string BookingConfirmedQueue { get; set; } = "booking-confirmed";
}
