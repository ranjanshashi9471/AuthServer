namespace AuthServer.Contracts.User.CurrentUser;

public sealed record CurrentUserResponse(Guid Id, string Email, string Username);