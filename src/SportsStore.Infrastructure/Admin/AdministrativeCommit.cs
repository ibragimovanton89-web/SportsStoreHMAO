using SportsStore.Infrastructure.Persistence;

namespace SportsStore.Infrastructure.Admin;

/// <summary>Внутренний обработчик успешной операции в её контексте и транзакции; не является параметром HTTP или формы.</summary>
/// <param name="db">Контекст сохраняемой операции.</param><param name="objectId">Идентификатор результата.</param><param name="ct">Отмена.</param>
public delegate Task AdministrativeCommit(ApplicationDbContext db, string objectId, CancellationToken ct);
