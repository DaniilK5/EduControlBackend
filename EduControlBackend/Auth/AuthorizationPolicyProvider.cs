using EduControlBackend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace EduControlBackend.Auth
{
    public class AuthorizationPolicyProvider : DefaultAuthorizationPolicyProvider
    {
        public AuthorizationPolicyProvider(IOptions<AuthorizationOptions> options) : base(options)
        {
        }

        public static void ConfigurePolicies(AuthorizationOptions options)
        {
            // Политики для Администратора
            options.AddPolicy(UserRole.Policies.ManageUsers, policy =>
                policy.RequireRole(UserRole.Administrator));

            options.AddPolicy(UserRole.Policies.ManageSettings, policy =>
                policy.RequireRole(UserRole.Administrator));

            // Политики для управления курсами

            options.AddPolicy(UserRole.Policies.ManageCourses, policy =>
                policy.RequireRole(UserRole.Administrator, UserRole.Teacher));

            // Политики для просмотра курсов

            options.AddPolicy(UserRole.Policies.ViewCourses, policy =>
                policy.RequireRole(UserRole.Administrator, UserRole.Teacher, UserRole.Student));

            // Политики для управления расписанием
           options.AddPolicy(UserRole.Policies.ManageSchedule, policy =>
                policy.RequireRole(UserRole.Administrator, UserRole.Teacher));

            options.AddPolicy(UserRole.Policies.ViewSchedule, policy =>
                policy.RequireRole(UserRole.Administrator, UserRole.Teacher, UserRole.Student, UserRole.Parent));

            // Политики для оценок
            options.AddPolicy(UserRole.Policies.ManageGrades, policy =>
                policy.RequireRole(UserRole.Administrator, UserRole.Teacher));

            options.AddPolicy(UserRole.Policies.ViewGrades, policy =>
                policy.RequireRole(UserRole.Administrator, UserRole.Teacher, UserRole.Student, UserRole.Parent));

            // Политики для сообщений
            options.AddPolicy(UserRole.Policies.SendMessages, policy =>
                policy.RequireAuthenticatedUser());

            options.AddPolicy(UserRole.Policies.DeleteMessages, policy =>
                policy.RequireRole(UserRole.Administrator));

            options.AddPolicy(UserRole.Policies.ManageGroupChats, policy =>
                policy.RequireRole(UserRole.Administrator, UserRole.Teacher));

            // Политики для отчетов
            options.AddPolicy(UserRole.Policies.ManageReports, policy =>
                policy.RequireRole(UserRole.Administrator, UserRole.Teacher));

            options.AddPolicy(UserRole.Policies.ViewReports, policy =>
                policy.RequireRole(UserRole.Administrator, UserRole.Teacher, UserRole.Student, UserRole.Parent));

            // Политики для заданий
            options.AddPolicy(UserRole.Policies.ManageAssignments, policy =>
                policy.RequireRole(UserRole.Administrator, UserRole.Teacher));

            options.AddPolicy(UserRole.Policies.ViewAssignments, policy =>
                policy.RequireRole(UserRole.Administrator, UserRole.Teacher, UserRole.Student, UserRole.Parent));

            options.AddPolicy(UserRole.Policies.SubmitAssignments, policy =>
                policy.RequireRole(UserRole.Student));
        }
    }
}