using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MyCal.Application.Calculators;
using MyCal.Domain.Entity;
using MyCal.Domain.Enum;
using MyCal.Shared.Tracking;
using MyCal.Web.Components.Tracking;
using System.Globalization;

namespace MyCal.Web.Components.Pages;

public partial class Log
{
    [Parameter]
    public int? LogId { get; set; }
    protected override Task OnParametersSetAsync() => Run(async () =>
    {
        if (LogId is { } logId)
        {
            var date = await Tracker.LegacyLogDateAsync(IdentityId, logId);
            if (date is null)
                Error = "That food log could not be found.";
            else
                Navigation.NavigateTo($"/diary/{date:yyyy-MM-dd}", replace: true);
        }
    });
}
