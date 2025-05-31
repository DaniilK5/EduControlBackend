using Microsoft.AspNetCore.Http;

namespace EduControlBackend.Services
{
    public class FileService
    {
        private readonly string _uploadPath;
        private readonly long _maxFileSize;
        private readonly string[] _allowedExtensions;

        public FileService(IConfiguration configuration)
        {
            _uploadPath = configuration["FileStorage:Path"] ?? "uploads";
            _maxFileSize = configuration.GetValue<long>("FileStorage:MaxFileSize", 104857600);
            _allowedExtensions = configuration.GetSection("FileStorage:AllowedExtensions")
                .Get<string[]>() ?? new[] { ".jpg", ".jpeg", ".png", ".pdf", ".doc", ".docx", ".txt" };

            if (!Directory.Exists(_uploadPath))
            {
                Directory.CreateDirectory(_uploadPath);
            }
        }

        public async Task<(string path, string fileName)> SaveFileAsync(IFormFile file)
        {
            try
            {
                if (file.Length > _maxFileSize)
                    throw new Exception($"Файл превышает максимальный размер {_maxFileSize / 1024 / 1024} МБ");

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!_allowedExtensions.Contains(extension))
                    throw new Exception("Неподдерживаемый тип файла");

                var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid()}{extension}";
                var relativePath = Path.Combine("messages", DateTime.UtcNow.ToString("yyyy/MM/dd"));
                var fullPath = Path.Combine(_uploadPath, relativePath);
                
                Directory.CreateDirectory(fullPath);

                var filePath = Path.Combine(fullPath, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return (Path.Combine(relativePath, fileName), file.FileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving file: {ex}");
                throw;
            }
        }

        public async Task<byte[]> GetFileAsync(string filePath)
        {
            var fullPath = Path.Combine(_uploadPath, filePath);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException();
            }

            return await File.ReadAllBytesAsync(fullPath);
        }


        public Stream? GetFile(string filePath)
        {
            var fullPath = Path.Combine(_uploadPath, filePath);
            return !System.IO.File.Exists(fullPath) ? null : System.IO.File.OpenRead(fullPath);
        }


        public void DeleteFile(string filePath)
        {
            var fullPath = Path.Combine(_uploadPath, filePath);
            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);
        }


        // Добавляем новые методы для работы с изображениями оценок и расписания
        public async Task<(string path, string fileName)> SaveGradeImageAsync(IFormFile file)
        {
            try
            {
                if (file.Length > _maxFileSize)
                    throw new Exception($"Файл превышает максимальный размер {_maxFileSize / 1024 / 1024} МБ");

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                var allowedImageExtensions = new[] { ".jpg", ".jpeg", ".png" };
                if (!allowedImageExtensions.Contains(extension))
                    throw new Exception("Неподдерживаемый тип файла. Разрешены только изображения (jpg, jpeg, png)");

                var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid()}{extension}";
                var relativePath = Path.Combine("grades", DateTime.UtcNow.ToString("yyyy/MM"));
                var fullPath = Path.Combine(_uploadPath, relativePath);

                Directory.CreateDirectory(fullPath);

                var filePath = Path.Combine(fullPath, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return (Path.Combine(relativePath, fileName), file.FileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving grade image: {ex}");
                throw;
            }
        }

        public async Task<(string path, string fileName)> SaveScheduleImageAsync(IFormFile file)
        {
            try
            {
                if (file.Length > _maxFileSize)
                    throw new Exception($"Файл превышает максимальный размер {_maxFileSize / 1024 / 1024} МБ");

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                var allowedImageExtensions = new[] { ".jpg", ".jpeg", ".png" };
                if (!allowedImageExtensions.Contains(extension))
                    throw new Exception("Неподдерживаемый тип файла. Разрешены только изображения (jpg, jpeg, png)");

                var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid()}{extension}";
                var relativePath = Path.Combine("schedules", DateTime.UtcNow.ToString("yyyy/MM"));
                var fullPath = Path.Combine(_uploadPath, relativePath);

                Directory.CreateDirectory(fullPath);

                var filePath = Path.Combine(fullPath, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return (Path.Combine(relativePath, fileName), file.FileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving schedule image: {ex}");
                throw;
            }
        }

        public async Task<byte[]?> GetGradeImageAsync(string filePath)
        {
            var fullPath = Path.Combine(_uploadPath, filePath);
            if (!File.Exists(fullPath))
                return null;

            // Проверяем, что файл находится в папке grades
            var relativePath = Path.GetRelativePath(_uploadPath, fullPath);
            if (!relativePath.StartsWith("grades", StringComparison.OrdinalIgnoreCase))
                return null;

            return await File.ReadAllBytesAsync(fullPath);
        }

        public async Task<byte[]?> GetScheduleImageAsync(string filePath)
        {
            var fullPath = Path.Combine(_uploadPath, filePath);
            if (!File.Exists(fullPath))
                return null;

            // Проверяем, что файл находится в папке schedules
            var relativePath = Path.GetRelativePath(_uploadPath, fullPath);
            if (!relativePath.StartsWith("schedules", StringComparison.OrdinalIgnoreCase))
                return null;

            return await File.ReadAllBytesAsync(fullPath);
        }

        public Stream? GetGradeImageStream(string filePath)
        {
            var fullPath = Path.Combine(_uploadPath, filePath);
            if (!File.Exists(fullPath))
                return null;

            // Проверяем, что файл находится в папке grades
            var relativePath = Path.GetRelativePath(_uploadPath, fullPath);
            if (!relativePath.StartsWith("grades", StringComparison.OrdinalIgnoreCase))
                return null;

            return File.OpenRead(fullPath);
        }

        public Stream? GetScheduleImageStream(string filePath)
        {
            var fullPath = Path.Combine(_uploadPath, filePath);
            if (!File.Exists(fullPath))
                return null;

            // Проверяем, что файл находится в папке schedules
            var relativePath = Path.GetRelativePath(_uploadPath, fullPath);
            if (!relativePath.StartsWith("schedules", StringComparison.OrdinalIgnoreCase))
                return null;

            return File.OpenRead(fullPath);
        }
    }
}