namespace AuthServer.Contracts.User.ChangePassword;

public sealed record ChangePasswordRequest(string OldPassword, string NewPassword);
