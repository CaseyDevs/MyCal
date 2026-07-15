namespace MyCal.ApiService.Abstractions;

public interface IQueryHandler<in TQuery, TResult>
{
    Task<TResult> HandleAsync(
        TQuery query,
        CancellationToken cancellationToken);
}