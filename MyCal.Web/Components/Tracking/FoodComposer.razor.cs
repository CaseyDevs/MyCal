using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MyCal.Application.Calculators;
using MyCal.Domain.Entity;
using MyCal.Domain.Enum;
using MyCal.Shared.Tracking;
using MyCal.Web.Components.Tracking;
using System.Globalization;
using System.ComponentModel.DataAnnotations;

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
    private FoodSearchMarket _market = FoodSearchMarket.UnitedKingdom;
    private CancellationTokenSource? _searchCancellation;
    private bool _searching;
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
        CancelSearch();
        await StopScanner();
        _tab = tab;
        _selected = null;
        Error = null;
        _notice = null;
        _foods = [];
        if (tab is "Recents" or "Favourites")
            await Run(async () => _foods = (await Tracker.SavedFoodsAsync(IdentityId, tab == "Favourites")).ToList());
    }
    private Task ChangeMarket() => _query.Trim().Length >= 2 ? Search() : Task.CompletedTask;
    private async Task Search()
    {
        CancelSearch();
        var cancellation = new CancellationTokenSource();
        _searchCancellation = cancellation;
        _searching = true;
        _selected = null;
        _foods = [];
        _notice = null;
        Error = null;
        try
        {
            IdentityId = await Account.IdAsync();
            await foreach (var page in FoodSearch.SearchProgressivelyAsync(_query, cancellation.Token, _market))
            {
                if (_searchCancellation != cancellation)
                    return;
                _foods = page.Foods.ToList();
                _notice = page.Notice;
                if (page.IsComplete && _foods.Count == 0)
                    _notice = (_notice + " No matching foods found. Try worldwide packaged foods, another term, or create a custom food.").Trim();
                await InvokeAsync(StateHasChanged);
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        catch (UnauthorizedAccessException) { Navigation.NavigateTo("/account/login", forceLoad: true); }
        catch (ValidationException e) { if (_searchCancellation == cancellation) Error = e.Message; }
        finally
        {
            if (_searchCancellation == cancellation)
            {
                _searching = false;
                _searchCancellation = null;
            }
            cancellation.Dispose();
        }
    }
    private void CancelSearch()
    {
        _searchCancellation?.Cancel();
        _searchCancellation = null;
        _searching = false;
    }
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
        CancelSearch();
        await StopScanner();
        await OnClose.InvokeAsync();
    }
    public async ValueTask DisposeAsync()
    {
        CancelSearch();
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
