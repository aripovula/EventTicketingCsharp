using EventTicketing.Api.Contracts;

namespace EventTicketing.Api.Services;

public class AuthService
{
    public Task<UserInfo?> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult<UserInfo?>(null);
    }
}
