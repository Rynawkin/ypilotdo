using Microsoft.AspNetCore.Mvc; // ✅ FromFormAttribute için gerekli
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;
using System.Reflection;

namespace Monolith.WebAPI.Infrastructure;

public class SwaggerFileOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var fileParameters = context.MethodInfo
            .GetParameters()
            .Where(p => p.ParameterType == typeof(IFormFile) || 
                       p.ParameterType == typeof(IFormFile[]) ||
                       p.ParameterType == typeof(List<IFormFile>))
            .ToList();

        if (!fileParameters.Any())
            return;

        // Form parametrelerini temizle
        operation.Parameters?.Clear();

        // Request body'yi multipart/form-data olarak ayarla
        var formParameters = new Dictionary<string, OpenApiSchema>();
        
        foreach (var parameter in context.MethodInfo.GetParameters())
        {
            if (parameter.ParameterType == typeof(IFormFile))
            {
                formParameters.Add(parameter.Name!, new OpenApiSchema
                {
                    Type = "string",
                    Format = "binary"
                });
            }
            else if (parameter.GetCustomAttribute<FromFormAttribute>() != null)
            {
                // FromForm attribute'u olan diğer parametreler
                formParameters.Add(parameter.Name!, new OpenApiSchema
                {
                    Type = GetSchemaType(parameter.ParameterType),
                    Nullable = IsNullable(parameter.ParameterType)
                });
            }
        }

        operation.RequestBody = new OpenApiRequestBody
        {
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["multipart/form-data"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "object",
                        Properties = formParameters,
                        Required = formParameters.Keys
                            .Where(k => !IsNullable(context.MethodInfo.GetParameters()
                                .First(p => p.Name == k).ParameterType))
                            .ToHashSet()
                    }
                }
            }
        };
    }

    private string GetSchemaType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (type == typeof(string))
            return "string";
        if (type == typeof(int) || type == typeof(long))
            return "integer";
        if (type == typeof(decimal) || type == typeof(float) || type == typeof(double))
            return "number";
        if (type == typeof(bool))
            return "boolean";
        if (type == typeof(DateTime))
            return "string";
        if (type.IsEnum)
            return "string";
        
        return "string";
    }

    private bool IsNullable(Type type)
    {
        if (type.IsValueType)
            return Nullable.GetUnderlyingType(type) != null;
        
        return true; // Reference types are nullable
    }
}