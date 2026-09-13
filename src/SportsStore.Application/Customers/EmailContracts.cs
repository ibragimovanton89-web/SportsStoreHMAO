namespace SportsStore.Application.Customers;

/// <summary>Письмо доверенного серверного сценария; тело не журналируется.</summary>
/// <param name="Recipient">Адрес получателя.</param><param name="Subject">Тема на русском.</param><param name="Body">Обычный текст без HTML.</param><param name="MessageId">Идентификатор для диагностики и подавления дублей транспортом.</param>
public sealed record CustomerEmail(string Recipient, string Subject, string Body, Guid MessageId);
/// <summary>Транспорт письма; не участвует в транзакциях бизнес-данных.</summary>
public interface ICustomerEmailTransport
{
    /// <summary>Отправляет или сохраняет тестовое письмо; транспорт не сообщает об успехе до фактического завершения.</summary>
    Task SendAsync(CustomerEmail message, CancellationToken ct = default);
}
/// <summary>Регистрация и одноразовые письма Identity без хранения открытых токенов в общей очереди.</summary>
public interface ICustomerRegistration
{
    /// <summary>Атомарно создаёт учётную запись и покупателя; публичный результат одинаков для уже занятой почты.</summary>
    Task RegisterAsync(string email, string password, string displayName, CancellationToken ct = default);
    /// <summary>Повторно посылает подтверждение, если оно применимо; существование адреса не раскрывается.</summary>
    Task ResendAsync(string email, CancellationToken ct = default);
    /// <summary>Посылает восстановление только подтверждённому покупателю; неизвестный адрес не выделяется ответом.</summary>
    Task ForgotAsync(string email, CancellationToken ct = default);
    /// <summary>Проверяет код подтверждения почты, привязанный к конкретному пользователю и назначению.</summary>
    Task<bool> ConfirmAsync(string userId, string encodedToken, CancellationToken ct = default);
    /// <summary>Меняет пароль по действующему токену; не подтверждает почту автоматически и отзывает прежние сессии.</summary>
    Task<bool> ResetAsync(string userId, string encodedToken, string password, CancellationToken ct = default);
}
