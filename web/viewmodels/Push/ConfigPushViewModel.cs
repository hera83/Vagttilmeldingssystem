namespace web.ViewModels.Push;

/// <summary>Svar (JSON) til push.js: er push sat op, og hvilken offentlig nøgle skal browseren bruge.</summary>
public class ConfigPushViewModel
{
    public bool Enabled { get; set; }
    public string PublicKey { get; set; } = string.Empty;
}
