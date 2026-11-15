using web.Managers.Reminders.Dtos;

namespace web.Managers.Reminders.Interfaces;

public interface IReminderManager
{
    /// <summary>Lægger påmindelser 48 og 24 timer før vagtstart i køen. Idempotent: kan køre igen og igen og parallelt.</summary>
    Task<ReminderScanResultDto> QueueDueAsync(CancellationToken cancellationToken = default);
}
