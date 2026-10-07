namespace EventTicketing.Api.Models;

public interface ITimestamped
{
    DateTime CreatedAt { get; set; }

    DateTime UpdatedAt { get; set; }
}
