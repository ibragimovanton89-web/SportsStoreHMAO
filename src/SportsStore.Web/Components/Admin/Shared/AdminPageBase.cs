using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using SportsStore.Application.Admin;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components.Routing;

namespace SportsStore.Web.Components.Admin.Shared;

/// <summary>Общие состояния интерфейса: защита от повторного нажатия, отмена, безопасные сообщения и сохранение формы при ошибке.</summary>
public abstract class AdminPageBase : ComponentBase, IDisposable
{
    /// <summary>Техническая диагностика серверных ошибок без содержимого форм и пользовательских секретов.</summary>
    [Inject] protected ILogger<AdminPageBase> Logger { get; set; } = default!;
    /// <summary>Сервис навигации для фильтров URL.</summary>
    [Inject] protected NavigationManager Navigation { get; set; } = default!;
    /// <summary>Стандартный диалог браузера для подтверждения ухода с несохранённой формы.</summary>
    [Inject] protected IJSRuntime JS { get; set; } = default!;
    /// <summary>Источник отмены при закрытии компонента.</summary>
    protected readonly CancellationTokenSource Cancellation = new();
    /// <summary>Признак ожидаемой операции; запрещает повторную отправку.</summary>
    protected bool Busy;
    /// <summary>Сохранённая форма отличается от последнего прочитанного состояния.</summary>
    protected bool Dirty;
    /// <summary>Понятное сообщение об ошибке без исключений и JSON.</summary>
    protected string? Error;
    /// <summary>Подтверждение завершённого изменения.</summary>
    protected string? Success;
    /// <summary>Версия идентификатора отправки формы; сохраняется при ошибке для безопасного повтора.</summary>
    protected Guid OperationId = Guid.NewGuid();
    /// <summary>Текст поиска из URL, сохраняемый при переходах страниц.</summary>
    [SupplyParameterFromQuery(Name = "q")] public string? Search { get; set; }
    /// <summary>Номер страницы из URL.</summary>
    [SupplyParameterFromQuery(Name = "page")] public int Page { get; set; } = 1;
    /// <summary>Сортировка из URL; сервис принимает только разрешённые ключи.</summary>
    [SupplyParameterFromQuery(Name = "sort")] public string? Sort { get; set; }
    /// <summary>Фильтр статуса из URL.</summary>
    [SupplyParameterFromQuery(Name = "filter")] public string? Filter { get; set; }
    /// <summary>Текущий ограниченный запрос списка.</summary>
    protected AdminQuery Query => new(Search ?? "", Math.Max(1, Page), Sort ?? "name", Filter ?? "");
    /// <summary>Выполняет серверное действие; исключение не стирает введённые пользователем значения.</summary>
    protected async Task RunAsync(Func<Task> work, bool mutation = false)
    {
        if (Busy) return; Busy = true; Error = null; Success = null;
        try { await work(); if (mutation) { Success = "Изменения сохранены."; Dirty = false; OperationId = Guid.NewGuid(); } }
        catch (OperationCanceledException) { Error = "Операция прервана. Обновите сведения, чтобы проверить результат."; }
        catch (DbUpdateConcurrencyException) { Error = "Запись изменена другим сотрудником. Ваши значения оставлены в форме. Скопируйте их и обновите сведения перед повтором."; }
        catch (UnauthorizedAccessException) { Error = "Права или сессия изменились. Выполните вход заново; действие не разрешено."; }
        catch (DbUpdateException) { Error = "Сохранение отклонено: проверьте уникальность, зависимости и актуальность данных. Обновите сведения."; }
        catch (Npgsql.PostgresException) { Error = "Конкурирующая операция или ограничение базы данных. Обновите сведения перед повтором."; }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException or InvalidDataException)
        { Logger.LogError(e, "Ошибка административной операции {OperationId}", OperationId); Error = HasRussian(e.Message) ? e.Message : "Не удалось выполнить действие. Проверьте заполнение, состояние партии и настройки расчёта."; }
        catch (Exception) { Error = "Сервис временно недоступен. Проверьте результат после обновления страницы."; }
        finally { Busy = false; }
    }
    /// <summary>Не показывает технические англоязычные сообщения библиотек в интерфейсе.</summary>
    private static bool HasRussian(string message) => message.Any(c => c is >= 'А' and <= 'я');
    /// <summary>Форматирует рубли русской локалью; неизвестное значение не становится нулём.</summary>
    protected static string Money(decimal? value) => value is null ? "—" : value.Value.ToString("N2", CultureInfo.GetCultureInfo("ru-RU")) + " ₽";
    /// <summary>Применяет фильтры через URL и сбрасывает страницу.</summary>
    protected virtual void SearchNow() => Navigation.NavigateTo(Navigation.GetUriWithQueryParameters(new Dictionary<string, object?> { ["q"] = Search, ["filter"] = Filter, ["sort"] = Sort, ["page"] = 1 }));
    /// <summary>Дожидается текущей операции перед поиском, чтобы быстрый ввод не терял обновление списка.</summary>
    protected async Task SearchWhenReady()
    {
        try
        {
            while (Busy) await Task.Delay(50, Cancellation.Token);
            if (!Dirty && !Cancellation.IsCancellationRequested) SearchNow();
        }
        catch (OperationCanceledException) when (Cancellation.IsCancellationRequested) { }
    }
    /// <summary>Переходит к соседней странице, сохраняя фильтры.</summary>
    protected void GoPage(int page) => Navigation.NavigateTo(Navigation.GetUriWithQueryParameter("page", Math.Max(1, page)));
    /// <summary>Отменяет внутренний переход, если сотрудник решил сохранить введённые данные.</summary>
    protected async Task BeforeNavigation(LocationChangingContext context)
    {
        if (Dirty && !await JS.InvokeAsync<bool>("confirm", "Есть несохранённые изменения. Покинуть страницу?")) context.PreventNavigation();
    }
    /// <summary>Отменяет незавершённые чтения при уходе со страницы.</summary>
    public void Dispose() { Cancellation.Cancel(); Cancellation.Dispose(); GC.SuppressFinalize(this); }
}
