using web.Repositories.PushSubscriptions.Dtos;

namespace web.Repositories.PushSubscriptions.Interfaces;

public interface IPushSubscriptionRepository
{
    /// <summary>Opretter abonnementet, eller opdaterer det, hvis adressen findes (også hvis det tilhørte en anden bruger på samme enhed).</summary>
    Task<PushSubscriptionDto> SaveAsync(SavePushSubscriptionDto dto, CancellationToken cancellationToken = default);

    /// <summary>Sletter brugerens eget abonnement med adressen. false, hvis det ikke findes eller tilhører en anden.</summary>
    Task<bool> DeleteForUserAsync(Guid userId, string endpoint, CancellationToken cancellationToken = default);

    /// <summary>Sletter et udløbet abonnement.</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<PushSubscriptionDto>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Aktive brugere med mindst ét abonnement.</summary>
    Task<List<Guid>> GetActiveSubscriberIdsAsync(CancellationToken cancellationToken = default);

    Task<bool> MarkSuccessAsync(Guid id, DateTime atUtc, CancellationToken cancellationToken = default);
}
