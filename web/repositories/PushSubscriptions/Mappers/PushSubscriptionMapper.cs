using System.Linq.Expressions;
using web.Data.Models;
using web.Repositories.PushSubscriptions.Dtos;

namespace web.Repositories.PushSubscriptions.Mappers;

public static class PushSubscriptionMapper
{
    public static readonly Expression<Func<PushSubscription, PushSubscriptionDto>> ToDtoExpression = x => new PushSubscriptionDto
    {
        Id = x.Id,
        UserId = x.UserId,
        Endpoint = x.Endpoint,
        P256dh = x.P256dh,
        Auth = x.Auth,
        LastSuccessAt = x.LastSuccessAt,
        CreatedAt = x.CreatedAt
    };

    private static readonly Func<PushSubscription, PushSubscriptionDto> CompiledToDto = ToDtoExpression.Compile();

    public static PushSubscriptionDto ToDto(this PushSubscription entity) => CompiledToDto(entity);

    public static PushSubscription ToEntity(this SavePushSubscriptionDto dto) => new()
    {
        UserId = dto.UserId,
        Endpoint = dto.Endpoint,
        P256dh = dto.P256dh,
        Auth = dto.Auth
    };

    public static void ApplyTo(this SavePushSubscriptionDto dto, PushSubscription entity)
    {
        entity.UserId = dto.UserId;
        entity.P256dh = dto.P256dh;
        entity.Auth = dto.Auth;
    }
}
