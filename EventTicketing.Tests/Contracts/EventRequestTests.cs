using System.ComponentModel.DataAnnotations;
using EventTicketing.Api.Contracts;

namespace EventTicketing.Tests.Contracts;

public class EventRequestTests
{
    private static EventRequest Valid() => new()
    {
        Title = "Jazz Night",
        Description = "Live jazz.",
        StartTime = new DateTime(2026, 11, 2, 23, 0, 0, DateTimeKind.Utc),
        EndTime = new DateTime(2026, 11, 3, 2, 0, 0, DateTimeKind.Utc),
        Venue = "Blue Note Club",
        EventType = "Music",
        TotalSeats = 120,
        AvailableSeats = 120,
        PriceCents = 2500,
        ImageUrl = "https://example.com/jazz.jpg",
    };

    private static List<ValidationResult> Validate(EventRequest request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
        return results;
    }

    private static string[] InvalidMembers(EventRequest request) =>
        Validate(request).SelectMany(r => r.MemberNames).ToArray();

    [Fact]
    public void AValidRequestHasNoErrors()
    {
        Assert.Empty(Validate(Valid()));
    }

    [Fact]
    public void EndTimeMustBeAfterStartTime()
    {
        var request = Valid() with { EndTime = Valid().StartTime };

        Assert.Equal(["EndTime"], InvalidMembers(request));
    }

    [Fact]
    public void AvailableSeatsCannotExceedTotalSeats()
    {
        var request = Valid() with { AvailableSeats = 121 };

        Assert.Equal(["AvailableSeats"], InvalidMembers(request));
    }

    [Fact]
    public void EventTypeMustBeAKnownCategory()
    {
        var request = Valid() with { EventType = "Opera" };

        Assert.Equal(["EventType"], InvalidMembers(request));
    }

    [Theory]
    [InlineData(0, 0, 2500)]
    [InlineData(120, -1, 2500)]
    [InlineData(120, 10, -1)]
    public void SeatsAndPriceMustBeInRange(int total, int available, int price)
    {
        var request = Valid() with { TotalSeats = total, AvailableSeats = available, PriceCents = price };

        Assert.NotEmpty(Validate(request));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(49)]
    public void APaidPriceMustBeAtLeastStripesMinimumCharge(int price)
    {
        Assert.Equal(["PriceCents"], InvalidMembers(Valid() with { PriceCents = price }));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    public void FreeAndFiftyCentPricesAreAllowed(int price)
    {
        Assert.Empty(Validate(Valid() with { PriceCents = price }));
    }

    [Fact]
    public void ImageUrlMustBeAUrlWhenGiven()
    {
        Assert.Equal(["ImageUrl"], InvalidMembers(Valid() with { ImageUrl = "not a url" }));
        Assert.Empty(Validate(Valid() with { ImageUrl = null }));
    }
}
