using Microsoft.EntityFrameworkCore;
using web.Data;
using web.Infrastructure.Authentication;
using web.Repositories.PasswordTokens.Dtos;
using web.Repositories.PasswordTokens.Interfaces;
using web.Repositories.PasswordTokens.Mappers;

namespace web.Repositories.PasswordTokens;

public class PasswordTokenRepository(WebDbContext dbContext) : IPasswordTokenRepository
{
    public async Task<IssuedPasswordTokenDto> CreateAsync(CreatePasswordTokenDto dto, CancellationToken cancellationToken = default)
    {
        var token = PasswordTokenGenerator.GenerateToken();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.PasswordTokens
            .Where(x => x.UserId == dto.UserId && x.Purpose == dto.Purpose && x.UsedAt == null)
            .ExecuteDeleteAsync(cancellationToken);
        var entity = dto.ToEntity(PasswordTokenGenerator.Hash(token));
        dbContext.PasswordTokens.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new IssuedPasswordTokenDto { PasswordToken = entity.ToDto(), Token = token };
    }

    public Task<PasswordTokenDto?> FindAsync(string token, CancellationToken cancellationToken = default)
    {
        var hash = PasswordTokenGenerator.Hash(token);
        return dbContext.PasswordTokens.AsNoTracking()
            .Where(x => x.TokenHash == hash)
            .Select(PasswordTokenMapper.ToDtoExpression)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> ConsumeAsync(Guid id, DateTime nowUtc, CancellationToken cancellationToken = default) =>
        // Én UPDATE med betingelserne: to samtidige forsøg kan ikke begge bruge linket.
        await dbContext.PasswordTokens
            .Where(x => x.Id == id && x.UsedAt == null && x.ExpiresAt > nowUtc)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.UsedAt, nowUtc)
                .SetProperty(x => x.UpdatedAt, nowUtc), cancellationToken) == 1;

    public Task<int> DeleteUnusedForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        dbContext.PasswordTokens.Where(x => x.UserId == userId && x.UsedAt == null).ExecuteDeleteAsync(cancellationToken);
}
