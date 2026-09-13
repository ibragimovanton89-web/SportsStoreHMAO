namespace SportsStore.Domain.Entities;
/// <summary>Базовая сущность: собственный идентификатор и версия для оптимистической блокировки без зависимости Domain от EF Core.</summary>
public abstract class Entity
{
    /// <summary>Уникальный идентификатор записи (первичный ключ).</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Системное поле PostgreSQL xmin: версия строки для защиты от конкурентного изменения; не дата и не время.</summary>
    public uint Version { get; set; }
}
/// <summary>Состояние собственной карточки товара.</summary>
public enum ProductStatus
{
    /// <summary>Черновик: не опубликован.</summary>
    Draft,
    /// <summary>Опубликованная карточка.</summary>
    Published,
    /// <summary>Архивная карточка.</summary>
    Archived,
}
/// <summary>Правовой тип покупателя; не определяет право на оптовую цену.</summary>
public enum CustomerKind
{
    /// <summary>Физическое лицо.</summary>
    Individual,
    /// <summary>Индивидуальный предприниматель.</summary>
    SoleProprietor,
    /// <summary>Организация.</summary>
    Organization,
}
/// <summary>Коммерческие условия покупателя или цены.</summary>
public enum CustomerSegment
{
    /// <summary>Розничные условия.</summary>
    Retail,
    /// <summary>Оптовые условия; требуют отдельного подтверждения покупателя.</summary>
    Wholesale,
}
/// <summary>Результат серверной проверки права покупателя на опт.</summary>
public enum WholesaleStatus
{
    /// <summary>Опт не запрошен.</summary>
    NotRequested,
    /// <summary>Заявка ожидает проверки.</summary>
    Pending,
    /// <summary>Оптовый статус подтверждён.</summary>
    Approved,
    /// <summary>Заявка отклонена.</summary>
    Rejected,
}
/// <summary>Состояние партии импорта.</summary>
public enum ImportStatus
{
    /// <summary>Предварительно проверена; предложения ещё не обновлены.</summary>
    Preview,
    /// <summary>Обнаружены ошибки; применение запрещено.</summary>
    Invalid,
    /// <summary>Транзакционно применена.</summary>
    Applied,
}
/// <summary>Классификация строки исходного прайса.</summary>
public enum ImportRowKind
{
    /// <summary>Пустая строка.</summary>
    Empty,
    /// <summary>Шапка, заголовок или исключённый идентичный повтор.</summary>
    Header,
    /// <summary>Раздел исходного прайса.</summary>
    Section,
    /// <summary>Корректная товарная строка.</summary>
    Product,
    /// <summary>Строка с ошибкой проверки.</summary>
    Error,
}
/// <summary>Состояние предложения пересчёта цены.</summary>
public enum ProposalStatus
{
    /// <summary>Ожидает применения.</summary>
    Pending,
    /// <summary>Применено к опубликованной цене.</summary>
    Applied,
    /// <summary>Устарело или заменено другим предложением.</summary>
    Superseded,
}
