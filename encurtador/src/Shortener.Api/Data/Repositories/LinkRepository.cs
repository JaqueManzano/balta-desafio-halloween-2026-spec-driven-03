using System.Data;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Shortener.Api.Data.Entities;

namespace Shortener.Api.Data.Repositories;

public enum RedirectStatus
{
    Redirected,
    NotFound,
    Expired
}

public sealed record RedirectResult(RedirectStatus Status, string? TargetUrl = null);

public sealed class LinkRepository(AppDbContext db)
{
    public async Task<bool> CreateAsync(Link link, CancellationToken cancellationToken)
    {
        db.Links.Add(link);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (IsCodeUniqueViolation(exception))
        {
            db.Entry(link).State = EntityState.Detached;
            return false;
        }
    }

    public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken) =>
        db.Links.AnyAsync(link => link.Code == code, cancellationToken);

    public async Task<RedirectResult> RedirectAndCountAsync(string code, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                UPDATE "Links"
                SET "AccessCount" = "AccessCount" + 1
                WHERE "Code" = $code AND "ExpiresAtUtc" > $now
                RETURNING "TargetUrl";
                """;
            var codeParameter = command.CreateParameter();
            codeParameter.ParameterName = "$code";
            codeParameter.Value = code;
            command.Parameters.Add(codeParameter);
            var nowParameter = command.CreateParameter();
            nowParameter.ParameterName = "$now";
            nowParameter.DbType = DbType.DateTime;
            nowParameter.Value = nowUtc;
            command.Parameters.Add(nowParameter);

            var targetUrl = await command.ExecuteScalarAsync(cancellationToken);
            if (targetUrl is string target)
            {
                return new RedirectResult(RedirectStatus.Redirected, target);
            }

            var link = await db.Links.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == code, cancellationToken);
            return link is null
                ? new RedirectResult(RedirectStatus.NotFound)
                : new RedirectResult(RedirectStatus.Expired);
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    public Task<List<Link>> ListByOwnerAsync(string ownerId, CancellationToken cancellationToken) =>
        db.Links.AsNoTracking()
            .Where(link => link.OwnerId == ownerId)
            .OrderByDescending(link => link.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public Task<Link?> FindByOwnerAsync(string code, string ownerId, CancellationToken cancellationToken) =>
        db.Links.AsNoTracking()
            .SingleOrDefaultAsync(link => link.Code == code && link.OwnerId == ownerId, cancellationToken);

    private static bool IsCodeUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is Microsoft.Data.Sqlite.SqliteException
        {
            SqliteErrorCode: 19
        } sqliteException &&
        sqliteException.Message.Contains("Links.Code", StringComparison.Ordinal);
}
