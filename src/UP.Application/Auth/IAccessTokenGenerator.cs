using System;
using System.Collections.Generic;
using System.Text;

namespace UP.Application.Auth;

public interface IAccessTokenGenerator
{
    AccessToken Generate(Guid userId, string email, IReadOnlyCollection<string> roles);
}

public sealed record AccessToken(string Value, DateTimeOffset ExpiresAt);