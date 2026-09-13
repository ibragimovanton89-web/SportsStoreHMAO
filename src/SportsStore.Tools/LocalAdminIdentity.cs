using SportsStore.Application.Admin;

namespace SportsStore.Tools;

/// <summary>Явный доверенный локальный сценарий только консольного процесса; тип не входит в зависимости Web и не выбирается входным флагом.</summary>
internal sealed class LocalAdminIdentity : IAdminIdentity
{
    /// <summary>Идентифицирует локального оператора для аудита без пользовательских секретов.</summary>
    public Task<AdminSession> GetAsync(CancellationToken ct = default) => Task.FromResult(new AdminSession("local-cli", "", true, Local: true));
}
