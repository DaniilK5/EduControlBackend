using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace EduControlBackend.Swagger
{
    public class FileUploadOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var fileParameters = context.MethodInfo.GetParameters()
                .Where(p => p.ParameterType == typeof(IFormFile) || 
                           (p.ParameterType == typeof(List<IFormFile>) || 
                            p.ParameterType == typeof(IEnumerable<IFormFile>)));

            foreach (var parameter in fileParameters)
            {
                var isFileArray = parameter.ParameterType != typeof(IFormFile);

                operation.RequestBody = new OpenApiRequestBody
                {
                    Required = true,
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["multipart/form-data"] = new OpenApiMediaType
                        {
                            Schema = new OpenApiSchema
                            {
                                Type = "object",
                                Properties = new Dictionary<string, OpenApiSchema>
                                {
                                    [parameter.Name] = isFileArray
                                        ? new OpenApiSchema
                                        {
                                            Type = "array",
                                            Items = new OpenApiSchema
                                            {
                                                Type = "string",
                                                Format = "binary"
                                            }
                                        }
                                        : new OpenApiSchema
                                        {
                                            Type = "string",
                                            Format = "binary"
                                        }
                                },
                                Required = parameter.GetCustomAttributes(typeof(RequiredAttribute), false).Any()
                                    ? new HashSet<string> { parameter.Name }
                                    : null
                            }
                        }
                    }
                };
            }
        }
    }
}