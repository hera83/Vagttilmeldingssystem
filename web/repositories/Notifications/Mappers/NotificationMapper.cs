using System.Linq.Expressions;
using web.Data.Models;
using web.Repositories.Notifications.Dtos;

namespace web.Repositories.Notifications.Mappers;

public static class NotificationMapper
{
    public static readonly Expression<Func<Notification, NotificationDto>> ToDtoExpression = x => new NotificationDto
    {
        Id = x.Id,
        UserId = x.UserId,
        Channel = x.Channel,
        Kind = x.Kind,
        Title = x.Title,
        Body = x.Body,
        Url = x.Url,
        DedupKey = x.DedupKey,
        Status = x.Status,
        Attempts = x.Attempts,
        NextAttemptAt = x.NextAttemptAt,
        SentAt = x.SentAt,
        LastError = x.LastError,
        CreatedAt = x.CreatedAt
    };

    private static readonly Func<Notification, NotificationDto> CompiledToDto = ToDtoExpression.Compile();

    public static NotificationDto ToDto(this Notification entity) => CompiledToDto(entity);

    public static Notification ToEntity(this CreateNotificationDto dto) => new()
    {
        UserId = dto.UserId,
        Channel = dto.Channel,
        Kind = dto.Kind,
        Title = dto.Title,
        Body = dto.Body,
        Url = dto.Url,
        DedupKey = dto.DedupKey,
        Status = NotificationStatus.Pending,
        NextAttemptAt = dto.NotBeforeUtc
    };
}
