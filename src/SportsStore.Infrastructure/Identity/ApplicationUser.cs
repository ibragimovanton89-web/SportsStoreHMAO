using Microsoft.AspNetCore.Identity;
namespace SportsStore.Infrastructure.Identity;

// Персональные поля добавляются только при обоснованной необходимости сценария.
/// <summary>Учётная запись ASP.NET Core Identity со строковым ключом; унаследованные поля аутентификации хранятся в AspNetUsers.</summary>
public sealed class ApplicationUser : IdentityUser { }

