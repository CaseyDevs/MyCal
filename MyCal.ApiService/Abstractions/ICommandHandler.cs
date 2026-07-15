namespace MyCal.ApiService.Abstractions;

public interface ICommandHandler<in TCommand, TResult>
{
    Task<TResult> HandleAsync(
        TCommand command,
        CancellationToken cancellationToken);
}