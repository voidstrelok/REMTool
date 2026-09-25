using Microsoft.EntityFrameworkCore;
using RemTool.Shared;
using RemTool.Shared.Enum;

namespace RemTool.Application.Admin;

public sealed record InformativoCommand(TipoInformativo Tipo, string Titulo, string Contenido,
    string? Url, string? TextoEnlace, DateOnly FechaPublicacion, bool Vigente, bool Destacado);

public sealed class InformativoService(RemToolDataContext db)
{
    public Task<List<Informativo>> ListAsync(CancellationToken ct) => db.Informativo
        .AsNoTracking().OrderByDescending(x => x.FechaPublicacion).ThenByDescending(x => x.Id).ToListAsync(ct);

    public async Task<Informativo> SaveAsync(int? id, InformativoCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Titulo)) throw new ArgumentException("El título es obligatorio.");
        if (string.IsNullOrWhiteSpace(command.Contenido)) throw new ArgumentException("El contenido es obligatorio.");
        var item = id is null ? new Informativo() : await db.Informativo.FindAsync([id.Value], ct)
            ?? throw new KeyNotFoundException("Informativo no encontrado.");
        item.Tipo = command.Tipo; item.Titulo = command.Titulo.Trim(); item.Contenido = command.Contenido.Trim();
        item.Url = string.IsNullOrWhiteSpace(command.Url) ? null : command.Url.Trim();
        item.TextoEnlace = string.IsNullOrWhiteSpace(command.TextoEnlace) ? null : command.TextoEnlace.Trim();
        item.FechaPublicacion = command.FechaPublicacion; item.Vigente = command.Vigente; item.Destacado = command.Destacado;
        if (id is null) db.Informativo.Add(item);
        await db.SaveChangesAsync(ct);
        return item;
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var item = await db.Informativo.FindAsync([id], ct) ?? throw new KeyNotFoundException("Informativo no encontrado.");
        db.Informativo.Remove(item); await db.SaveChangesAsync(ct);
    }
}
