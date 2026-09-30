namespace ExpenseWatch.Api.Models;

public sealed record AuthRequest(string? Email, string? Password);
public sealed record TokenResponse(string AccessToken, string TokenType, DateTimeOffset ExpiresAt);
public sealed record AuthError(string Error);
public sealed record AuthValidationError(string Code, string Description);
public sealed record AuthErrors(AuthValidationError[] Errors);
