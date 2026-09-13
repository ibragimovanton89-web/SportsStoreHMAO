using System.ComponentModel.DataAnnotations;
namespace SportsStore.Web.Configuration;

/// <summary>Проверяемые при старте публичные настройки названия магазина и региона обслуживания.</summary>
public sealed class StoreOptions
{
    /// <summary>Имя раздела конфигурации с публичными настройками магазина.</summary>
    public const string SectionName = "Store";
    /// <summary>Публичное название магазина.</summary>
    [Required] public string Name { get; set; } = "";
    /// <summary>Регион обслуживания, отображаемый в шапке сайта.</summary>
    [Required] public string Region { get; set; } = "";
}

