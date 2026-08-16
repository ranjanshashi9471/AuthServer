namespace AuthServer.Contracts.Authentication.ResetPassword;

public sealed record ResetPasswordRequest(string AccessToken, string NewPassword);
