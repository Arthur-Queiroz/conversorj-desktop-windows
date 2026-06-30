namespace ConversorJ.Core;

public interface IYtDlpRunner
{
    Task<CommandResult> RunAsync(IEnumerable<string> arguments, CancellationToken cancellationToken);
}
