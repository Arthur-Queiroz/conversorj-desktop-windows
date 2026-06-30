namespace ConversorJ.Core;

public interface IWhisperRunner
{
    Task<CommandResult> RunAsync(IEnumerable<string> arguments, CancellationToken cancellationToken);
}
