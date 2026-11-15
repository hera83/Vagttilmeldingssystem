using web.Managers.Enrollment.Dtos;

namespace web.Managers.Enrollment.Interfaces;

public interface IEnrollmentManager
{
    /// <summary>De åbne vagter (aldrig kladder) med ledige pladser og brugerens egne markeret.</summary>
    Task<AvailableOverviewDto> GetAvailableAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Brugerens samlede tilmelding: kræver komplet profil og er alt eller intet (krav K02, K13, K14).</summary>
    Task<EnrollResultDto> EnrollAsync(Guid userId, IReadOnlyCollection<Guid> shiftIds, CancellationToken cancellationToken = default);

    Task<HomeOverviewDto> GetHomeAsync(Guid userId, CancellationToken cancellationToken = default);
}
