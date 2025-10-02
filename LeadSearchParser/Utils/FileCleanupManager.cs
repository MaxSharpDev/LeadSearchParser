using System.IO;

namespace LeadSearchParser.Utils;

/// <summary>
/// Управление очисткой старых файлов результатов
/// </summary>
public class FileCleanupManager
{
    private readonly string _outputFolder;
    private readonly int _keepDays;
    private readonly Logger _logger;

    public FileCleanupManager(string outputFolder, int keepDays, Logger logger)
    {
        _outputFolder = outputFolder;
        _keepDays = keepDays;
        _logger = logger;
    }

    /// <summary>
    /// Очистить старые файлы
    /// </summary>
    public void CleanupOldFiles()
    {
        try
        {
            if (!Directory.Exists(_outputFolder))
            {
                return;
            }

            var cutoffDate = DateTime.Now.AddDays(-_keepDays);
            var extensions = new[] { "*.xlsx", "*.csv", "*.json" };
            var deletedCount = 0;
            var deletedSize = 0L;

            foreach (var extension in extensions)
            {
                var files = Directory.GetFiles(_outputFolder, extension);
                
                foreach (var file in files)
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        
                        if (fileInfo.LastWriteTime < cutoffDate)
                        {
                            var size = fileInfo.Length;
                            File.Delete(file);
                            deletedCount++;
                            deletedSize += size;
                            
                            _logger.Info($"Удален старый файл: {Path.GetFileName(file)}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Warning($"Не удалось удалить {Path.GetFileName(file)}: {ex.Message}");
                    }
                }
            }

            if (deletedCount > 0)
            {
                var sizeMB = deletedSize / (1024.0 * 1024.0);
                _logger.Success($"Очистка завершена: удалено {deletedCount} файл(ов), освобождено {sizeMB:F2} МБ");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Ошибка при очистке файлов: {ex.Message}");
        }
    }

    /// <summary>
    /// Получить информацию о файлах
    /// </summary>
    public void ShowStorageInfo()
    {
        try
        {
            if (!Directory.Exists(_outputFolder))
            {
                _logger.Info($"Папка {_outputFolder} не существует");
                return;
            }

            var extensions = new[] { "*.xlsx", "*.csv", "*.json" };
            var totalFiles = 0;
            var totalSize = 0L;
            var oldFiles = 0;
            var cutoffDate = DateTime.Now.AddDays(-_keepDays);

            foreach (var extension in extensions)
            {
                var files = Directory.GetFiles(_outputFolder, extension);
                totalFiles += files.Length;
                
                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    totalSize += fileInfo.Length;
                    
                    if (fileInfo.LastWriteTime < cutoffDate)
                    {
                        oldFiles++;
                    }
                }
            }

            var sizeMB = totalSize / (1024.0 * 1024.0);
            
            Console.WriteLine();
            _logger.Info("═══════════════════════════════════════");
            _logger.Info("Информация о хранилище результатов:");
            _logger.Info("═══════════════════════════════════════");
            _logger.Info($"Папка: {Path.GetFullPath(_outputFolder)}");
            _logger.Info($"Всего файлов: {totalFiles}");
            _logger.Info($"Размер: {sizeMB:F2} МБ");
            _logger.Info($"Старых файлов (>{_keepDays} дн.): {oldFiles}");
            
            if (oldFiles > 0)
            {
                _logger.Warning($"Рекомендуется очистка! Запустите: --cleanup");
            }
            else
            {
                _logger.Success("Очистка не требуется");
            }
            
            _logger.Info("═══════════════════════════════════════");
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            _logger.Error($"Ошибка получения информации: {ex.Message}");
        }
    }

    /// <summary>
    /// Очистить ВСЕ файлы (принудительная очистка)
    /// </summary>
    public void CleanupAllFiles()
    {
        try
        {
            if (!Directory.Exists(_outputFolder))
            {
                _logger.Warning($"Папка {_outputFolder} не существует");
                return;
            }

            var extensions = new[] { "*.xlsx", "*.csv", "*.json" };
            var deletedCount = 0;

            foreach (var extension in extensions)
            {
                var files = Directory.GetFiles(_outputFolder, extension);
                
                foreach (var file in files)
                {
                    try
                    {
                        File.Delete(file);
                        deletedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.Warning($"Не удалось удалить {Path.GetFileName(file)}: {ex.Message}");
                    }
                }
            }

            _logger.Success($"Удалено {deletedCount} файл(ов)");
        }
        catch (Exception ex)
        {
            _logger.Error($"Ошибка при очистке: {ex.Message}");
        }
    }

    /// <summary>
    /// Проверить, нужна ли очистка при запуске
    /// </summary>
    public bool ShouldAutoCleanup()
    {
        if (!Directory.Exists(_outputFolder))
            return false;

        var extensions = new[] { "*.xlsx", "*.csv", "*.json" };
        var cutoffDate = DateTime.Now.AddDays(-_keepDays);
        var oldFilesCount = 0;

        foreach (var extension in extensions)
        {
            var files = Directory.GetFiles(_outputFolder, extension);
            oldFilesCount += files.Count(f => new FileInfo(f).LastWriteTime < cutoffDate);
        }

        return oldFilesCount > 0;
    }
}

