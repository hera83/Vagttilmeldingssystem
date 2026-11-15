using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using web.Data;
using web.Data.Models;
using web.Repositories.Users.Dtos;
using web.Repositories.Users.Interfaces;
using web.Repositories.Users.Mappers;

namespace web.Repositories.Users;

public class UserRepository(WebDbContext dbContext) : IUserRepository
{
    private static readonly PasswordHasher<User> Hasher = new();

    // Bruges, når brugernavnet ikke findes, så svartiden ikke afslører, om brugeren findes.
    private static readonly string DummyHash = Hasher.HashPassword(new User(), Guid.NewGuid().ToString());

    public Task<bool> AnyAsync(CancellationToken cancellationToken = default) =>
        dbContext.Users.AnyAsync(cancellationToken);

    public Task<List<UserDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        dbContext.Users.AsNoTracking()
            .OrderBy(x => x.UserName)
            .Select(UserMapper.ToDtoExpression)
            .ToListAsync(cancellationToken);

    public Task<UserDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Users.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(UserMapper.ToDtoExpression)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<UserDto?> ValidateCredentialsAsync(string userName, string password, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Users.FirstOrDefaultAsync(x => x.UserName == userName.Trim(), cancellationToken);
        if (entity is null || !entity.IsActive)
        {
            Hasher.VerifyHashedPassword(new User(), DummyHash, password);
            return null;
        }

        var result = Hasher.VerifyHashedPassword(entity, entity.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed)
        {
            return null;
        }

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            entity.PasswordHash = Hasher.HashPassword(entity, password);
        }

        entity.LastLoginAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.ToDto();
    }

    public async Task<UserDto?> CreateAsync(CreateUserDto dto, CancellationToken cancellationToken = default)
    {
        if (await dbContext.Users.AnyAsync(x => x.UserName == dto.UserName.Trim(), cancellationToken))
        {
            return null;
        }

        var entity = dto.ToEntity();
        entity.PasswordHash = Hasher.HashPassword(entity, dto.Password);
        dbContext.Users.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.ToDto();
    }

    public async Task<UserDto?> UpdateAsync(Guid id, UpdateUserDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        dto.ApplyTo(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.ToDto();
    }

    public async Task<ChangeUserNameResult> ChangeUserNameAsync(Guid id, string newUserName, CancellationToken cancellationToken = default)
    {
        var userName = newUserName.Trim();
        var entity = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return ChangeUserNameResult.NotFound;
        }

        if (entity.UserName == userName)
        {
            return ChangeUserNameResult.Unchanged;
        }

        // NOCASE: en anden bruger med samme e-mail i andre bogstaver tæller også.
        if (await dbContext.Users.AnyAsync(x => x.Id != id && x.UserName == userName, cancellationToken))
        {
            return ChangeUserNameResult.Taken;
        }

        entity.UserName = userName;
        entity.SecurityStamp = Guid.NewGuid();
        await dbContext.SaveChangesAsync(cancellationToken);
        return ChangeUserNameResult.Success;
    }

    public async Task<bool> SetPasswordAsync(Guid id, string newPassword, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        entity.PasswordHash = Hasher.HashPassword(entity, newPassword);
        entity.SecurityStamp = Guid.NewGuid();
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<UserDto?> ChangePasswordAsync(Guid id, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null || Hasher.VerifyHashedPassword(entity, entity.PasswordHash, currentPassword) == PasswordVerificationResult.Failed)
        {
            return null;
        }

        entity.PasswordHash = Hasher.HashPassword(entity, newPassword);
        entity.SecurityStamp = Guid.NewGuid();
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.ToDto();
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Users.Where(x => x.Id == id).ExecuteDeleteAsync(cancellationToken) > 0;
}
