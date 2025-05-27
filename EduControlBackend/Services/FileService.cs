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
    }
}