using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SportsStore.Infrastructure;
using SportsStore.Infrastructure.Identity;
using SportsStore.Infrastructure.Persistence;

namespace SportsStore.Tools;

/// <summary>Однократное локальное создание первого администратора без встроенного пароля или повышения существующей учётной записи.</summary>
internal static class BootstrapAdmin
{
    /// <summary>Запрашивает скрытый пароль и явное локальное подтверждение; повторный запуск не меняет аккаунты.</summary>
    internal static async Task<int> RunAsync(IConfiguration configuration)
    {
        if (Console.IsInputRedirected) throw new InvalidOperationException("Bootstrap требует интерактивного терминала со скрытым вводом.");
        var services = new ServiceCollection(); services.AddLogging(); services.AddInfrastructure(configuration);
        await using var provider = services.BuildServiceProvider(); await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(); var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(861002)");
        if (await (from ur in db.UserRoles join r in db.Roles on ur.RoleId equals r.Id where r.Name == "Admin" select ur).AnyAsync())
        { Console.WriteLine("Администратор уже существует. Аккаунты и пароли не изменены."); return 0; }
        Console.Write("Email первого администратора: "); var email = Console.ReadLine()?.Trim() ?? "";
        if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email) || email.Length > 256) throw new ArgumentException("Некорректный email.");
        if (await users.FindByEmailAsync(email) is not null || await users.FindByNameAsync(email) is not null)
            throw new InvalidOperationException("Аккаунт уже существует. Bootstrap не повышает существующие аккаунты.");
        Console.Write("Пароль (ввод скрыт): "); var password = ReadSecret();
        Console.Write("Повторите пароль: "); if (password != ReadSecret()) throw new ArgumentException("Пароли не совпадают.");
        Console.Write("Подтверждаете личность сотрудника и владение указанным email локально? Введите ПОДТВЕРЖДАЮ: ");
        if (Console.ReadLine() != "ПОДТВЕРЖДАЮ") throw new InvalidOperationException("Создание отменено.");
        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, LockoutEnabled = true };
        var result = await users.CreateAsync(user, password);
        if (!result.Succeeded) throw new InvalidOperationException("Пароль не соответствует политике: от 12 символов, разные регистры, цифра, специальный знак и 4 разных символа.");
        result = await users.AddToRoleAsync(user, "Admin"); if (!result.Succeeded) throw new InvalidOperationException("Не удалось назначить роль Admin.");
        db.AdminAudits.Add(new() { ActorId = "local-bootstrap", Action = "staff.bootstrap", ObjectType = "ApplicationUser", ObjectId = user.Id,
            OperationId = Guid.NewGuid(), Description = "Первый Admin создан после явного локального подтверждения личности; email не отправлялся." });
        await db.SaveChangesAsync(); await tx.CommitAsync();
        Console.WriteLine("Первый Admin создан. Войдите через /account/login и обязательно настройте TOTP."); return 0;
    }

    /// <summary>Читает пароль с клавиатуры без отображения, включая поддержку удаления последнего символа.</summary>
    private static string ReadSecret()
    {
        var chars = new List<char>();
        while (true)
        {
            var key = Console.ReadKey(true); if (key.Key == ConsoleKey.Enter) { Console.WriteLine(); return new string(chars.ToArray()); }
            if (key.Key == ConsoleKey.Backspace) { if (chars.Count > 0) chars.RemoveAt(chars.Count - 1); }
            else if (!char.IsControl(key.KeyChar) && chars.Count < 1024) chars.Add(key.KeyChar);
        }
    }
}
