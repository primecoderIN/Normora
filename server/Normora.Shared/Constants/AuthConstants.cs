namespace Normora.Shared.Constants;

public static class AuthConstants
{
    // Cookies
    public const string DevCookieName = "normora-auth";
    public const string ProdCookieName = "__Host-spa";

    // Clients
    public const string WebClientId = "normora-web";
    
    // Scopes
    public const string OpenIdScope = "openid";
    public const string ProfileScope = "profile";
    public const string EmailScope = "email";
    
    // Claims
    public const string PreferredUsernameClaim = "preferred_username";
    public const string RoleClaim = "role";
    
    // Auth Properties
    public const string ProviderProperty = "provider";
    public const string KeycloakIdpHintParameter = "kc_idp_hint";
}
