using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using SportsStore.Application.Commerce;
using SportsStore.Application.Customers;
using SportsStore.Domain.Entities;
namespace SportsStore.Web.Pages.Commerce;
/// <summary>HTTP-формы корзины и оформления; POST защищён antiforgery, цены и CustomerId не привязываются.</summary>
/// <param name="commerce">Серверные транзакционные операции.</param><param name="accounts">Собственные адреса покупателя.</param>
[EnableRateLimiting("customer-account")]
public sealed class CheckoutModel(ICommerce commerce, ICustomerAccount accounts) : PageModel
{
    /// <summary>Режим страницы cart/checkout/orders/order, установленный маршрутом.</summary>
    public string Mode => Request.Path.StartsWithSegments("/customer/orders") ? Id == Guid.Empty ? "orders" : "order" : Request.Path.StartsWithSegments("/checkout") ? "checkout" : "cart";
    /// <summary>Идентификатор собственного заказа, не замена авторизации.</summary>
    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    /// <summary>Идентификатор сохранённого preview.</summary>
    [BindProperty(SupportsGet = true)] public Guid PreviewId { get; set; }
    /// <summary>Выбранный вариант команды корзины.</summary>
    [BindProperty] public Guid VariantId { get; set; }
    /// <summary>Желаемое целое количество; ноль только для удаления.</summary>
    [BindProperty] public int Quantity { get; set; } = 1;
    /// <summary>Ожидаемая версия корзины или заказа.</summary>
    [BindProperty] public uint Version { get; set; }
    /// <summary>Ключ повторной отправки сохраняется при ошибке.</summary>
    [BindProperty] public Guid OperationId { get; set; } = Guid.NewGuid();
    /// <summary>Выбранный адрес с серверной проверкой владельца.</summary>
    [BindProperty] public Guid AddressId { get; set; }
    /// <summary>Номер страницы истории.</summary>
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    /// <summary>Публичное сообщение без технического исключения.</summary>
    public string? Error { get; private set; }
    /// <summary>Пересчитанная корзина.</summary>
    public CartView? Cart { get; private set; }
    /// <summary>Подтверждаемые условия.</summary>
    public PreviewView? Preview { get; private set; }
    /// <summary>Адреса текущего владельца.</summary>
    public IReadOnlyList<AddressInput> Addresses { get; private set; } = [];
    /// <summary>Личная история.</summary>
    public OrderPage? Orders { get; private set; }
    /// <summary>Снимок собственного заказа.</summary>
    public CustomerOrderView? Order { get; private set; }
    /// <summary>Название страницы.</summary>
    public string Title => Mode switch { "checkout" => "Подтверждение заказа", "orders" => "Мои заказы", "order" => "Заказ " + Order?.Number, _ => "Корзина" };
    /// <summary>Загружает данные только разрешённого маршрута.</summary>
    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        try
        {
            await Load(ct);
            // Повторный GET того же preview (обновление/Back) сохраняет ключ логического оформления.
            if (Mode == "checkout" && PreviewId != Guid.Empty) OperationId = PreviewId;
            return Page();
        }
        catch (UnauthorizedAccessException) { return Mode == "order" ? NotFound() : Redirect("/customer/login?returnUrl=" + Uri.EscapeDataString(Mode == "checkout" ? "/checkout" : "/customer/orders")); }
        catch (ArgumentException ex) { Error = ex.Message; return Page(); }
        catch (Exception) { Error = "Сервис временно недоступен. Попробуйте позже."; Response.StatusCode = 503; return Page(); }
    }
    /// <summary>Обрабатывает ограниченный набор команд; данные формы не определяют владельца или стоимость.</summary>
    public async Task<IActionResult> OnPostAsync(string? command, CancellationToken ct)
    {
        try
        {
            if (!ModelState.IsValid) throw new ArgumentException("Проверьте значения формы.");
            switch (Mode, command)
            {
                case ("cart", "add"): await commerce.AddAsync(VariantId, Quantity, OperationId, ct); return Redirect("/cart");
                case ("cart", "set"): await commerce.SetAsync(VariantId, Quantity, Version, ct); return Redirect("/cart");
                case ("checkout", "preview"):
                    var p = await commerce.PreviewAsync(AddressId, ct); return Redirect("/checkout?PreviewId=" + p.Id);
                case ("checkout", "submit"):
                    var result = await commerce.SubmitAsync(PreviewId, OperationId, ct);
                    if (result.OrderId is Guid id) return Redirect("/customer/orders/" + id + "?created=true");
                    PreviewId = result.NewPreviewId!.Value; ModelState.Remove(nameof(PreviewId)); Error = result.Message; break;
                case ("order", "cancel"):
                    await commerce.ChangeOrderAsync(Id, Version, "cancel", "", null, OperationId, false, ct); return Redirect("/customer/orders/" + Id);
                default: return BadRequest();
            }
        }
        catch (UnauthorizedAccessException) { return Redirect("/customer/login?returnUrl=" + Uri.EscapeDataString("/checkout")); }
        catch (DbUpdateConcurrencyException) { Error = "Данные изменены в другой вкладке. Обновите страницу и проверьте количество перед повтором."; }
        catch (ArgumentException ex) { Error = ex.Message; }
        catch (Exception) { Error = "Операция временно недоступна. Проверьте историю заказов перед повторной отправкой."; }
        try { await Load(ct); } catch (Exception) { Error ??= "Не удалось прочитать данные."; }
        return Page();
    }
    /// <summary>Читает только собственные данные; не изменяет состав при открытии страницы.</summary>
    private async Task Load(CancellationToken ct)
    {
        switch (Mode)
        {
            case "cart": Cart = await commerce.CartAsync(ct); break;
            case "checkout":
                Addresses = await accounts.AddressesAsync(ct); Cart = await commerce.CartAsync(ct);
                if (PreviewId != Guid.Empty) Preview = await commerce.PreviewAsync(PreviewId, true, ct); break;
            case "orders": Orders = await commerce.OrdersAsync(false, null, null, PageNumber, ct); break;
            case "order": Order = await commerce.OrderAsync(Id, false, ct); break;
            default: throw new ArgumentException("Страница недоступна.");
        }
    }
    /// <summary>Статус не выдаёт подтверждение за оплату или отправку.</summary>
    public static string Status(CustomerOrderStatus status) => status switch { CustomerOrderStatus.Historical => "Исторический заказ", CustomerOrderStatus.AwaitingConfirmation => "Ожидает подтверждения", CustomerOrderStatus.Confirmed => "Подтверждён сотрудником", CustomerOrderStatus.Cancelled => "Отменён", _ => "Резерв истёк" };
    /// <summary>Отображает время ХМАО явно, независимо от часового пояса сервера.</summary>
    public static string When(DateTime? value) => value is null ? "—" : new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)).ToOffset(TimeSpan.FromHours(5)).ToString("dd.MM.yyyy HH:mm") + " (UTC+5)";
}
