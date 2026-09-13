/** Скачивает сформированную сервером книгу через поток Blazor, без публичного URL заказа.
 * @param {string} fileName Безопасное имя, сформированное сервером.
 * @param {object} streamReference Ссылка DotNetStreamReference на книгу Excel.
 * @returns {Promise<void>} Завершение передачи потока и запуска браузерного скачивания.
 */
export async function downloadPurchase(fileName, streamReference) {
    const buffer = await streamReference.arrayBuffer();
    const blob = new Blob([buffer], { type: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    try { link.click(); }
    finally {
        link.remove();
        // Даём браузеру начать чтение Blob перед освобождением адреса.
        setTimeout(() => URL.revokeObjectURL(url), 1000);
    }
}
