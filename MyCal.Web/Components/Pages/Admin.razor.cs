using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MyCal.Application.Calculators;
using MyCal.Domain.Entity;
using MyCal.Domain.Enum;
using MyCal.Shared.Tracking;
using MyCal.Web.Components.Tracking;
using System.Globalization;

namespace MyCal.Web.Components.Pages;

public partial class Admin
{
    private bool _admin, _recovery; private string _email = ""; private string? _link; private ElementReference _linkInput; [Inject] private IJSRuntime JS { get; set; } = null!; protected override Task OnInitializedAsync() => Run(async () => { _admin = (await Account.GetAsync()).IsAdmin; if (!_admin) Error = "Administrator access is required."; }); private Task Issue() => Run(async () => { var token = await Account.IssueLinkAsync(_email, _recovery); _link = Navigation.BaseUri + "account/accept?token=" + Uri.EscapeDataString(token); }); private Task SelectLink() => _linkInput.FocusAsync().AsTask();
}
