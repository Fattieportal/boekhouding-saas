using Boekhouding.Application.Interfaces;
using Scriban;
using Scriban.Runtime;
using System.Reflection;
using System.Collections;

namespace Boekhouding.Infrastructure.Services;

/// <summary>
/// Template renderer implementatie met Scriban (Liquid-like syntax)
/// </summary>
public class ScribanTemplateRenderer : ITemplateRenderer
{
    public async Task<string> RenderAsync(string template, object data)
    {
        try
        {
            var scribanTemplate = Template.Parse(template);
            
            if (scribanTemplate.HasErrors)
            {
                var errors = string.Join(", ", scribanTemplate.Messages.Select(m => m.Message));
                throw new InvalidOperationException($"Template parsing errors: {errors}");
            }
            
            // Create a script object and add all data recursively
            var scriptObject = new ScriptObject();
            scriptObject.Import(data, renamer: member => member.Name);
            
            var context = new TemplateContext();
            context.StrictVariables = false; // Allow undefined variables
            context.PushGlobal(scriptObject);
            
            var result = await scribanTemplate.RenderAsync(context);
            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Template rendering failed: {ex.Message}", ex);
        }
    }

    private void AddObjectToScriptObject(ScriptObject scriptObject, object data)
    {
        if (data == null) return;

        var properties = data.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var prop in properties)
        {
            var value = prop.GetValue(data);
            
            if (value == null)
            {
                scriptObject.Add(prop.Name, null);
            }
            else if (value is DateTime dateTime)
            {
                // DateTime should be formatted as string to avoid conversion issues
                scriptObject.Add(prop.Name, dateTime.ToString("dd-MM-yyyy"));
            }
            else if (value is string || value.GetType().IsPrimitive || value.GetType().IsValueType)
            {
                // Simple types - add directly
                scriptObject.Add(prop.Name, value);
            }
            else if (value is IEnumerable enumerable and not string)
            {
                // Collections - convert to list of script objects
                var list = new List<ScriptObject>();
                foreach (var item in enumerable)
                {
                    var itemScript = new ScriptObject();
                    AddObjectToScriptObject(itemScript, item);
                    list.Add(itemScript);
                }
                scriptObject.Add(prop.Name, list);
            }
            else
            {
                // Complex objects - convert to script object
                var nestedScript = new ScriptObject();
                AddObjectToScriptObject(nestedScript, value);
                scriptObject.Add(prop.Name, nestedScript);
            }
        }
    }
}
