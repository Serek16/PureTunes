using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;

namespace EmyProject.Shared;

public partial class MainLayoutComponent : LayoutComponentBase
{
    [Inject]
    protected NotificationService NotificationService { get; set; }

    protected RadzenBody body;
    protected RadzenHeader header;
}