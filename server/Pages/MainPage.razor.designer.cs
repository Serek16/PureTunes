using System.Collections.Generic;
using EmyProject.CustomService.Model;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Configuration;
using Radzen;

namespace EmyProject.Pages;

public partial class MainPageComponent : ComponentBase
{
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, dynamic> Attributes { get; set; }

    [Inject]
    protected IJSRuntime JSRuntime { get; set; }
    
    [Inject]
    protected NotificationService NotificationService { get; set; }
    
    [Inject] protected IConfiguration Configuration { get; set; }

    protected string DirectoryPath { get; set; }

    protected List<PathModel> DirectoryPathCollection { get; set; }
    
    protected List<PathModel> FilePathCollection { get; set; } 
    
    protected string FilePath { get; set; }

    protected List<PathModel> ExaminedFilePathList { get; set; }

    protected double Confidence { get; set; }

    protected string ResultJson { get; set; }

    private string _datasetPath;
    
    private string _examinedFileDirectoryPath;

    protected bool IsTaskRunning;
    
    protected async System.Threading.Tasks.Task Load()
    {
        _datasetPath = Configuration["Dataset"];
        _examinedFileDirectoryPath = Configuration["ExaminedFileDataset"];
        
        DirectoryPath = string.Empty;
        
        DirectoryPathCollection = GetDirectories();

        FilePath = string.Empty;

        ExaminedFilePathList = GetExaminedFiles();

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
        IsTaskRunning = true;
        ResultJson = await GetResult();
        IsTaskRunning = false;
    }
}