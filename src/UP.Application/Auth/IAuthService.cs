namespace UP.Application.Auth;

public interface IAuthService
{
    Task<RegistrationResult> RegisterAsync(string email, string password);
}

public sealed record RegistrationResult(bool Succeeded, IReadOnlyList<string> Errors)
{
    public static RegistrationResult Success { get; } = new(true, []);

    public static RegistrationResult Failure(IEnumerable<string> errors) => new(false, [.. errors]);
}
