namespace api.services
{
    /// <summary>Configuracao do Riot Sign On. Enabled=false ate a Riot liberar o client RSO.</summary>
    public record RsoOptions(
        bool Enabled,
        string ClientId,
        string ClientSecret,
        string RedirectUri,
        string RegionalHost,   // americas | europe | asia
        string FrontendUrl);
}
