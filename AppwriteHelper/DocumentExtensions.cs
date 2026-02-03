using AppwriteHelper.Models;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AppwriteHelper
{
    public static class DocumentExtensions
    {
        public static string ToJsonPropertyJson<T>(this T obj, Expression<Func<T, object>> propertySelector) where T : DocumentData
        {
            var selectedProperties = new Dictionary<string, object>();
            var body = propertySelector.Body;

            if (body is NewExpression newExpression)
            {
                foreach (var argument in newExpression.Arguments)
                {
                    ProcessMemberExpression(argument, obj, selectedProperties);
                }
            }
            else if (body is MemberExpression memberExpression)
            {
                ProcessMemberExpression(memberExpression, obj, selectedProperties);
            }
            else if (body is UnaryExpression unaryExpression)
            {
                ProcessMemberExpression(unaryExpression.Operand, obj, selectedProperties);
            }

            return JsonSerializer.Serialize(selectedProperties);
        }

        private static void ProcessMemberExpression(Expression expression, object obj, Dictionary<string, object> properties)
        {
            if (expression is MemberExpression memberExpression)
            {
                var propInfo = memberExpression.Member as PropertyInfo;
                if (propInfo != null)
                {
                    var jsonName = propInfo.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? propInfo.Name;
                    var value = propInfo.GetValue(obj);
                    properties[jsonName] = value;
                }
            }
        }
    }
}
