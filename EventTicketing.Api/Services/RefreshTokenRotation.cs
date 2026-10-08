using EventTicketing.Api.Contracts;

namespace EventTicketing.Api.Services;

public record RefreshTokenRotation(UserInfo User, string Token);
