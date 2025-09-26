namespace EduControlBackend.Models.AdminModels
{
    public class AppSettings
    {
        public int Id { get; set; }
        public string SiteName { get; set; } = "EduControl";
        public string DefaultTimeZone { get; set; } = "UTC+3";
        public int MaxFileSize { get; set; } = 10485760; // 10MB в байтах
        public string[] AllowedFileTypes { get; set; } = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".jpg", ".png" };
        public int MaxUploadFilesPerMessage { get; set; } = 5;
        public int DefaultPageSize { get; set; } = 20;
        public bool RequireEmailVerification { get; set; } = true;
        public int PasswordMinLength { get; set; } = 8;
        public bool RequireStrongPassword { get; set; } = true;
    }
}