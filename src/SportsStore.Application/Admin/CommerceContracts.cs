using SportsStore.Domain.Entities;

namespace SportsStore.Application.Admin;

/// <summary>Снимок закупочного тарифа для осознанного подтверждения.</summary>
/// <param name="Id">Идентификатор поставщика.</param><param name="Version">Версия.</param><param name="Name">Имя поставщика.</param>
/// <param name="TierId">Выбранный тариф.</param><param name="Confirmed">Подтверждение доступа.</param>
public sealed record SupplierData(Guid Id, uint Version, string Name, Guid? TierId, bool Confirmed);

/// <summary>Правило наценки для формы; приоритет определяется областью, а не порядком ввода.</summary>
/// <param name="Id">Правило или пустой GUID для нового.</param><param name="Version">Версия.</param><param name="Segment">Сегмент.</param>
/// <param name="CategoryId">Категория либо null.</param><param name="VariantId">Вариант либо null.</param><param name="Minimum">Нижняя граница количества SKU.</param><param name="Percent">Наценка к стоимости, не маржа.</param>
public sealed record MarkupData(Guid Id, uint Version, CustomerSegment Segment, Guid? CategoryId, Guid? VariantId, decimal Minimum, decimal Percent);

/// <summary>Результат подготовки цен с конкретными препятствиями вместо пустого успеха.</summary>
/// <param name="Ids">Подготовленные предложения.</param><param name="Problems">Причины отсутствия расчёта.</param>
public sealed record ProposalResult(IReadOnlyList<Guid> Ids, IReadOnlyList<string> Problems);

/// <summary>Строка предложения пересчёта, готовая для отображения без JSON.</summary>
/// <param name="Id">Предложение.</param><param name="Sku">SKU.</param><param name="Name">Название товара.</param><param name="Segment">Тип цены.</param>
/// <param name="Minimum">Граница ступени.</param><param name="Amount">Новая цена.</param><param name="Current">Текущая цена.</param>
/// <param name="Status">Состояние.</param><param name="Explanation">Читаемое объяснение расчёта.</param>
public sealed record ProposalData(Guid Id, string Sku, string Name, CustomerSegment Segment, decimal Minimum, decimal Amount, decimal? Current, ProposalStatus Status, string Explanation);

/// <summary>Проекция истории собственных цен.</summary>
/// <param name="At">UTC-время.</param><param name="Sku">SKU.</param><param name="Segment">Сегмент.</param><param name="Minimum">Ступень.</param>
/// <param name="Old">Прежняя цена.</param><param name="New">Новая цена.</param><param name="Source">Безопасное описание источника.</param>
public sealed record PriceHistoryData(DateTime At, string Sku, CustomerSegment Segment, decimal Minimum, decimal? Old, decimal New, string Source);

/// <summary>Общие команды цен Web и доверенного локального CLI с аудитом.</summary>
public interface IAdminCommerce
{
    /// <summary>Возвращает ограниченный список поставщиков.</summary>
    Task<IReadOnlyList<SupplierData>> SuppliersAsync(CancellationToken ct = default);
    /// <summary>Выбирает связанное предложение источником цены; Manager ограничен черновиками.</summary>
    Task SetSourceAsync(Guid variantId, uint version, Guid offerId, Guid operationId, CancellationToken ct = default);
    /// <summary>Возвращает страницу правил наценки.</summary>
    Task<AdminPage<MarkupData>> RulesAsync(AdminQuery query, CancellationToken ct = default);
    /// <summary>Сохраняет правило с проверкой области, ступени и версии.</summary>
    Task<Guid> SaveRuleAsync(MarkupData rule, Guid operationId, CancellationToken ct = default);
    /// <summary>Создаёт предложения с диагностикой; Manager никогда не публикует автоматически.</summary>
    Task<ProposalResult> ProposeAsync(Guid offerId, Guid operationId, CancellationToken ct = default);
    /// <summary>Возвращает страницу предложений цен.</summary>
    Task<AdminPage<ProposalData>> ProposalsAsync(AdminQuery query, CancellationToken ct = default);
    /// <summary>Публикует актуальное предложение, сохраняя защиту ручной цены.</summary>
    Task ApplyPriceAsync(Guid proposalId, Guid operationId, CancellationToken ct = default);
    /// <summary>Устанавливает ручную защищённую цену.</summary>
    Task ManualAsync(Guid variantId, CustomerSegment segment, decimal minimum, decimal amount, Guid operationId, CancellationToken ct = default);
    /// <summary>Возвращает ограниченную историю собственных цен.</summary>
    Task<AdminPage<PriceHistoryData>> HistoryAsync(AdminQuery query, CancellationToken ct = default);
}
