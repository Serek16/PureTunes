﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using PureTunes.Models;
using PureTunes.Services;
using Radzen;

namespace PureTunes.Pages;

public partial class MainPageComponent : ComponentBase
{
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, dynamic> Attributes { get; set; }

    [Inject]
    protected IJSRuntime JSRuntime { get; set; }
    
    [Inject]
    protected NotificationService NotificationService { get; set; }
    
    [Inject] protected IConfiguration Configuration { get; set; }

    [Inject] protected AFMService AfmService { get; set; }

    [Inject] protected AudioExtractionService AudioExtractionService { get; set; }

    [Inject] protected ILogger<MainPageComponent> Logger { get; set; }

    protected List<PathModel> DirectoryPathCollection { get; set; }
    
    protected List<PathModel> FilePathCollection { get; set; } 
    
    protected string FilePath { get; set; }

    protected List<PathModel> ExaminedFilePathList { get; set; }

    protected double Confidence { get; set; }
    
    private string _examinedFileDirectoryPath;

    protected bool IsFingerprintingAndMatching;

    protected bool IsExctracting;

    protected override async Task OnInitializedAsync()
    {
        _examinedFileDirectoryPath = Configuration["ExaminedFileDataset"];

        FilePath = string.Empty;

        ExaminedFilePathList = GetExaminedFiles();

        Confidence = 80; // Suggested optimal confidence value.
    }

    protected async Task Dropdown1Change(dynamic args)
    {
        FilePath = args;
    }
    
    protected async Task Button1Click(MouseEventArgs args)
    {
        await IsFilePathCorrect(FilePath);
    }

    protected async Task Button2Click(MouseEventArgs args)
    {
        await FindMatches();
    }

    protected async Task GenerateButtonClick(MouseEventArgs arg)
    {
        await ExtractAudioClips(ResultList);
    }
}