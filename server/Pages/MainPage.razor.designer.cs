using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen;
using Radzen.Blazor;

namespace EmyProject.Pages
{
    public partial class MainPageComponent : ComponentBase
    {
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, dynamic> Attributes { get; set; }

        [Inject]
        protected IJSRuntime JSRuntime { get; set; }

        [Inject]
        protected NavigationManager UriHelper { get; set; }

        [Inject]
        protected DialogService DialogService { get; set; }

        [Inject]
        protected TooltipService TooltipService { get; set; }

        [Inject]
        protected ContextMenuService ContextMenuService { get; set; }

        [Inject]
        protected NotificationService NotificationService { get; set; }
        
        protected List<EmyProject.CustomService.Model.PathModel> PathCollection { get; set; }

        protected string Path { get; set; }
        
        protected string FilePath { get; set; }
        
        protected double Confidence { get; set; }
        
        protected string ResultJson { get; set; }

        protected override async System.Threading.Tasks.Task OnInitializedAsync()
        {
            await Load();
        }
        
        protected async System.Threading.Tasks.Task Load()
        {
            var getCatalogResult = GetCatalog();
            PathCollection = getCatalogResult;

            Path = string.Empty;

            FilePath = string.Empty;

            Confidence = 80; // Suggested optimal confidence value.

            ResultJson = string.Empty;
        }

        protected async System.Threading.Tasks.Task Dropdown0Change(dynamic args)
        {
            Path = args;
        }

        protected async System.Threading.Tasks.Task Button0Click(MouseEventArgs args)
        {
            await UpdateEmy();
        }

        protected async System.Threading.Tasks.Task Button1Click(MouseEventArgs args)
        {
            await CheckFile();
        }

        protected async System.Threading.Tasks.Task Button2Click(MouseEventArgs args)
        {
            var getResultResult = await GetResult();
            ResultJson = getResultResult;
        }
    }
}