using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;

namespace EmyProject.Shared;

public partial class MainLayoutComponent : LayoutComponentBase
{
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

    protected RadzenBody body;
    protected RadzenHeader header;
}