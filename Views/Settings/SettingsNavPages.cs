using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace ImageHost.Views.Settings
{
    public static class SettingsNavPages
    {
        public static string ActivePageKey => "ActivePage";
        public static string Index => "Index";
        public static string AwsSettings => "AwsSettings";
        public static string TinifySettings => "TinifySettings";
        


        public static string IndexNavClass(ViewContext viewContext) => PageNavClass(viewContext, Index);
        public static string AwsSettingsNavClass(ViewContext viewContext) => PageNavClass(viewContext, AwsSettings);

        public static string TinifySettingsNavClass(ViewContext viewContext) =>
            PageNavClass(viewContext, TinifySettings);
        

        public static string PageNavClass(ViewContext viewContext, string page)
        {
            var activePage = viewContext.ViewData["ActivePage"] as string;
            return string.Equals(activePage, page, StringComparison.OrdinalIgnoreCase) ? "active" : null;
        }

        public static void AddActivePage(this ViewDataDictionary viewData, string activePage) => viewData[ActivePageKey] = activePage;
    }
}