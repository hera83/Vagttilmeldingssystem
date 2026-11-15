using System.Linq.Expressions;
using web.Data.Models;
using web.Repositories.Profiles.Dtos;

namespace web.Repositories.Profiles.Mappers;

public static class ProfileMapper
{
    /// <summary>Det ene sted, der afgør, om en profil er komplet. Kan oversættes til SQL.</summary>
    public static readonly Expression<Func<Profile, bool>> IsCompleteExpression = x =>
        x.FullName != "" && x.Address != "" && x.Phone != "" && x.ShirtSize != "" && x.ChildName != "" && x.ChildClass != "";

    public static readonly Expression<Func<Profile, ProfileDto>> ToDtoExpression = x => new ProfileDto
    {
        UserId = x.UserId,
        FullName = x.FullName,
        Address = x.Address,
        Phone = x.Phone,
        ShirtSize = x.ShirtSize,
        DrinkTickets = x.DrinkTickets,
        TivoliSheets = x.TivoliSheets,
        ChildName = x.ChildName,
        ChildClass = x.ChildClass,
        IsComplete = x.FullName != "" && x.Address != "" && x.Phone != "" && x.ShirtSize != "" && x.ChildName != "" && x.ChildClass != "",
        UpdatedAt = x.UpdatedAt
    };

    private static readonly Func<Profile, ProfileDto> CompiledToDto = ToDtoExpression.Compile();

    public static ProfileDto ToDto(this Profile entity) => CompiledToDto(entity);

    public static Profile ToEntity(this SaveProfileDto dto, Guid userId)
    {
        var entity = new Profile { UserId = userId };
        dto.ApplyTo(entity);
        return entity;
    }

    public static void ApplyTo(this SaveProfileDto dto, Profile entity)
    {
        entity.FullName = dto.FullName.Trim();
        entity.Address = dto.Address.Trim();
        entity.Phone = dto.Phone.Trim();
        entity.ShirtSize = dto.ShirtSize.Trim();
        entity.DrinkTickets = dto.DrinkTickets;
        entity.TivoliSheets = dto.TivoliSheets;
        entity.ChildName = dto.ChildName.Trim();
        entity.ChildClass = dto.ChildClass.Trim();
    }
}
