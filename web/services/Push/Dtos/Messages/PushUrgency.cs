namespace web.Services.Push.Dtos.Messages;

/// <summary>Hvor hurtigt push-tjenesten skal levere (fx vækker High en telefon i strømsparetilstand).</summary>
public enum PushUrgency
{
    Normal,
    High
}
