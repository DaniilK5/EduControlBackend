namespace EduControlBackend.Models.AdminModels
{
    public class UpdateSettingsDto
    {
        public string SiteName { get; set; }
        public string DefaultTimeZone { get; set; }
        public int MaxFileSize { get; set; }
        public string[] AllowedFileTypes { get; set; }
        public int MaxUploadFilesPerMessage { get; set; }
        public int DefaultPageSize { get; set; }
        public bool RequireEmailVerification { get; set; }
        public int PasswordMinLength { get; set; }
        public bool RequireStrongPassword { get; set; }
    }
}