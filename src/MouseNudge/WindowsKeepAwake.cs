using System.ComponentModel;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

namespace MouseNudge;

internal sealed class WindowsKeepAwake : IDisposable
{
    private const uint EsSystemRequired = 0x00000001;
    private const uint EsDisplayRequired = 0x00000002;
    private const uint EsContinuous = 0x80000000;

    private readonly ManualResetEventSlim _started = new(false);
    private readonly ManualResetEventSlim _stop = new(false);
    private readonly Thread _worker;
    private readonly uint _executionState;
    private Exception? _startupException;
    private bool _disposed;

    private WindowsKeepAwake(KeepAwakeOptions options)
    {
        _executionState = EsContinuous;

        if (options.KeepSystemAwake)
        {
            _executionState |= EsSystemRequired;
        }

        if (options.KeepDisplayOn)
        {
            _executionState |= EsDisplayRequired;
        }

        _worker = new Thread(Run)
        {
            IsBackground = true,
            Name = "MouseNudge.KeepAwake"
        };

        _worker.Start();
        _started.Wait();

        if (_startupException is not null)
        {
            Dispose();
            ExceptionDispatchInfo.Capture(_startupException).Throw();
        }
    }

    public static WindowsKeepAwake? Start(KeepAwakeOptions options) =>
        options.Enabled ? new WindowsKeepAwake(options) : null;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _stop.Set();

        if (_worker.IsAlive)
        {
            _worker.Join();
        }

        _started.Dispose();
        _stop.Dispose();
    }

    private void Run()
    {
        try
        {
            if (SetThreadExecutionState(_executionState) == 0)
            {
                throw new Win32Exception(
                    Marshal.GetLastWin32Error(),
                    "Windows did not accept the keep-awake request.");
            }
        }
        catch (Exception exception)
        {
            _startupException = exception;
        }
        finally
        {
            _started.Set();
        }

        if (_startupException is not null)
        {
            return;
        }

        _stop.Wait();

        // Execution-state requests are tracked per thread and must be cleared by the same thread.
        _ = SetThreadExecutionState(EsContinuous);
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint SetThreadExecutionState(uint executionState);
}
