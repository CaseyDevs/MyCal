using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MyCal.Application.Calculators;
using MyCal.Domain.Entity;
using MyCal.Domain.Enum;
using MyCal.Shared.Tracking;
using MyCal.Web.Components.Tracking;
using System.Globalization;

namespace MyCal.Web.Components.Tracking;

public partial class FoodComposer
{
    [Parameter] public Meal InitialMeal { get; set; } = Meal.Snacks;
    [Parameter]
    public EventCallback<FoodSelection> OnAdd { get; set; }
    [Parameter]
    public EventCallback OnClose { get; set; }
    private string _tab = "Recents", _query = "", _barcode = "";
    private string? _notice;
    private List<FoodItem> _foods = [];
    private FoodItem? _selected;
    private FoodInput _custom = new();
    private decimal _amount = 100;
    private Meal _meal;
    private NutritionUnit _basisUnit;
    private bool _basisConfirmed, _scanning;
    private readonly string _videoId = "scan-" + Guid.NewGuid().ToString("N");
    private IJSObjectReference? _scanner;
    private IJSObjectReference? _dialog;
    private readonly string _dialogId = "dialog-" + Guid.NewGuid().ToString("N");
    private DotNetObjectReference<FoodComposer>? _reference;
    private FoodItem EffectiveFood => _selected! with { Unit = _selected!.RequiresBasisConfirmation ? _basisUnit : _selected.Unit, RequiresBasisConfirmation = false };
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;
        _reference ??= DotNetObjectReference.Create(this);
        _dialog = await JS.InvokeAsync<IJSObjectReference>("import", "/js/dialog.js");
        await _dialog.InvokeVoidAsync("open", "#" + _dialogId, _reference);
    }
    [JSInvokable] public Task DismissDialog() => Close();
    protected override async Task OnInitializedAsync()
    {
        _meal = InitialMeal;
        await Run(async () => _foods = (await Tracker.SavedFoodsAsync(IdentityId)).ToList());
    }
    private async Task ChangeTab(string tab)
    {
        await StopScanner();
        _tab = tab;
        _selected = null;
        Error = null;
        _notice = null;
        _foods = [];
        if (tab is "Recents" or "Favourites")
            await Run(async () => _foods = (await Tracker.SavedFoodsAsync(IdentityId, tab == "Favourites")).ToList());
    }
    private Task Search() => Run(async () => { _selected = null; var result = await FoodSearch.SearchAsync(_query); _foods = result.Foods.ToList(); _notice = result.Notice; if (_foods.Count == 0) _notice = (_notice + " No matching foods found. Try another term or create a custom food.").Trim(); });
    private void Select(FoodItem food)
    {
        _selected = food;
        _amount = food.ReferenceQuantity;
        _basisUnit = food.Unit;
        _basisConfirmed = false;
    }
    private void SelectCustom()
    {
        Select(_custom.ToFood());
    }
    private Task Add() => Run(async () => { await OnAdd.InvokeAsync(new(EffectiveFood, _amount, _meal)); });
    private Task ToggleFavourite() => Run(async () => { var food = EffectiveFood; await Tracker.SaveFoodAsync(IdentityId, food, !food.IsFavourite); _selected = food with { IsFavourite = !food.IsFavourite }; });
    private Task LookupBarcode() => Run(async () => { await StopScanner(); var food = await FoodSearch.BarcodeAsync(_barcode); if (food is null) _notice = "No product found. Try a custom food using the package values."; else Select(food); });
    private async Task StartScanner()
    {
        Error = null;
        try
        {
            _scanner ??= await JS.InvokeAsync<IJSObjectReference>("import", "/js/scanner.js");
            _reference ??= DotNetObjectReference.Create(this);
            await _scanner.InvokeVoidAsync("start", _videoId, _reference);
            _scanning = true;
        }
        catch (JSException) { Error = "The camera couldn't open. Allow camera access over HTTPS or localhost, or enter the barcode manually."; }
    }
    [JSInvokable]
    public async Task BarcodeDetected(string code)
    {
        _barcode = code;
        await LookupBarcode();
        await InvokeAsync(StateHasChanged);
    }
    private async Task StopScanner()
    {
        if (_scanner is not null)
        {
            try
            {
                await _scanner.InvokeVoidAsync("stop");
            }
            catch (JSDisconnectedException) { }
        }
        _scanning = false;
    }
    private async Task Close()
    {
        if (Busy)
            return;
        await StopScanner();
        await OnClose.InvokeAsync();
    }
    public async ValueTask DisposeAsync()
    {
        if (_dialog is not null)
        {
            try
            {
                await _dialog.InvokeVoidAsync("close");
                await _dialog.DisposeAsync();
            }
            catch (JSDisconnectedException) { }
        }
        await StopScanner();
        _reference?.Dispose();
        if (_scanner is not null)
        {
            try
            {
                await _scanner.DisposeAsync();
            }
            catch (JSDisconnectedException) { }
        }
    }
    private static string Known(decimal? source, decimal value) => source is null ? "unknown" : value.ToString("0.#") + " g";
    public sealed record FoodSelection(FoodItem Food, decimal Amount, Meal Meal);
}
