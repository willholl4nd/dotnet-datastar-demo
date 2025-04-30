using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace dotnet_html_sortable_table.Extensions;

public static class ControllerExtensions
{
    public static async Task<string> RenderViewToStringAsync(this Controller controller, string viewName, object model, bool isPartial = true)
    {
        var controllerContext = controller.ControllerContext;
        var actionContext = controller.ControllerContext as ActionContext;

        var serviceProvider = controllerContext.HttpContext.RequestServices;
        var razorViewEngine = serviceProvider.GetService(typeof(IRazorViewEngine)) as IRazorViewEngine;
        var tempDataProvider = serviceProvider.GetService(typeof(ITempDataProvider)) as ITempDataProvider;

        using (var sw = new StringWriter())
        {
            var viewResult = razorViewEngine.FindView(actionContext, viewName, !isPartial);

            if (viewResult?.View == null)
                throw new ArgumentException($"{viewName} does not match any available view");
    
            var viewDictionary =
                new ViewDataDictionary(new EmptyModelMetadataProvider(),
                        new ModelStateDictionary())
                    { Model = model };

            var viewContext = new ViewContext(
                actionContext,
                viewResult.View,
                viewDictionary,
                new TempDataDictionary(actionContext.HttpContext, tempDataProvider),
                sw,
                new HtmlHelperOptions()
            );

            await viewResult.View.RenderAsync(viewContext);
            return sw.ToString();
        }
    }
}
