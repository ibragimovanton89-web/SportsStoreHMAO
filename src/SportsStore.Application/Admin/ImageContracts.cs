namespace SportsStore.Application.Admin;

/// <summary>Метаданные изображения карточки без физического пути сервера.</summary>
/// <param name="Id">Изображение.</param><param name="Version">Версия формы.</param><param name="Url">Безопасный адрес выдачи.</param><param name="Order">Порядок отображения.</param>
public sealed record ImageData(Guid Id, uint Version, string Url, int Order);

/// <summary>Управление фотографиями карточки с проверкой содержимого и прав.</summary>
public interface IAdminImages
{
    /// <summary>Возвращает изображения выбранной карточки.</summary>
    Task<IReadOnlyList<ImageData>> ListAsync(Guid productId, CancellationToken ct = default);
    /// <summary>Проверяет и перекодирует JPEG/PNG/WebP; физический путь не принимается от клиента.</summary>
    Task UploadAsync(Guid productId, uint productVersion, Stream content, Guid operationId, CancellationToken ct = default);
    /// <summary>Удаляет разрешённое изображение с проверкой версии.</summary>
    Task DeleteAsync(Guid id, uint version, Guid operationId, CancellationToken ct = default);
    /// <summary>Перемещает изображение на одну позицию; направление только -1 или 1.</summary>
    Task MoveAsync(Guid id, uint version, int direction, Guid operationId, CancellationToken ct = default);
}
