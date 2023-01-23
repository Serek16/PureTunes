using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using EmyProject.CustomService.Model;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen;
using Radzen.Blazor;

namespace EmyProject.Pages;

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

    protected string DirectoryPath { get; set; }

    protected List<PathModel> DirectoryPathCollection { get; set; }
    
    protected List<PathModel> FilePathCollection { get; set; } 
    
    protected string FilePath { get; set; }

    protected List<PathModel> ExaminedFilePathList { get; set; }

    protected double Confidence { get; set; }

    protected string ResultJson { get; set; }

    protected async System.Threading.Tasks.Task Load()
    {
        DirectoryPath = string.Empty;
        
        DirectoryPathCollection = GetDirectories();

        FilePath = string.Empty;

        ExaminedFilePathList = GetExaminedFilePathList();

        Confidence = 80; // Suggested optimal confidence value.

        ResultJson = string.Empty;
    }
    
    protected override async System.Threading.Tasks.Task OnInitializedAsync()
    {
        await Load();
    }

    protected async System.Threading.Tasks.Task Dropdown0Change(dynamic args)
    {
        DirectoryPath = args;
    }

    protected async System.Threading.Tasks.Task Button0Click(MouseEventArgs args)
    {
        await UpdateEmy();
    }

    protected async System.Threading.Tasks.Task Dropdown1Change(dynamic args)
    {
        FilePath = args;
    }
    
    protected async System.Threading.Tasks.Task Button1Click(MouseEventArgs args)
    {
        await CheckFile();
    }

    protected async System.Threading.Tasks.Task Button2Click(MouseEventArgs args)
    {
        ResultJson = await GetResult();
    }
}