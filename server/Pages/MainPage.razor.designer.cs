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

        public void Reload()
        {
            InvokeAsync(StateHasChanged);
        }

        public void OnPropertyChanged(PropertyChangedEventArgs args)
        {
        }

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

        List<EmyProject.CustomService.Model.PathModel> _PathCollection;
        protected List<EmyProject.CustomService.Model.PathModel> PathCollection
        {
            get
            {
                return _PathCollection;
            }
            set
            {
                if (!object.Equals(_PathCollection, value))
                {
                    var args = new PropertyChangedEventArgs(){ Name = "PathCollection", NewValue = value, OldValue = _PathCollection };
                    _PathCollection = value;
                    OnPropertyChanged(args);
                    Reload();
                }
            }
        }

        string _Path;
        protected string Path
        {
            get
            {
                return _Path;
            }
            set
            {
                if (!object.Equals(_Path, value))
                {
                    var args = new PropertyChangedEventArgs(){ Name = "Path", NewValue = value, OldValue = _Path };
                    _Path = value;
                    OnPropertyChanged(args);
                    Reload();
                }
            }
        }

        string _FilePath;
        protected string FilePath
        {
            get
            {
                return _FilePath;
            }
            set
            {
                if (!object.Equals(_FilePath, value))
                {
                    var args = new PropertyChangedEventArgs(){ Name = "FilePath", NewValue = value, OldValue = _FilePath };
                    _FilePath = value;
                    OnPropertyChanged(args);
                    Reload();
                }
            }
        }

        double _Confidence;
        protected double Confidence
        {
            get
            {
                return _Confidence;
            }
            set
            {
                if (!object.Equals(_Confidence, value))
                {
                    var args = new PropertyChangedEventArgs(){ Name = "Confidence", NewValue = value, OldValue = _Confidence };
                    _Confidence = value;
                    OnPropertyChanged(args);
                    Reload();
                }
            }
        }

        string _ResultJson;
        protected string ResultJson
        {
            get
            {
                return _ResultJson;
            }
            set
            {
                if (!object.Equals(_ResultJson, value))
                {
                    var args = new PropertyChangedEventArgs(){ Name = "ResultJson", NewValue = value, OldValue = _ResultJson };
                    _ResultJson = value;
                    OnPropertyChanged(args);
                    Reload();
                }
            }
        }

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

            Confidence = 0;

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
