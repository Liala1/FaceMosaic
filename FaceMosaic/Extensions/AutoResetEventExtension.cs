namespace FaceMosaic.Extensions;

public static class AutoResetEventExtension
{
	public static Task WaitOneAsync(this AutoResetEvent are, CancellationToken cancellationToken)
	{
		var tcs = new TaskCompletionSource<bool>();
		var registration = cancellationToken.Register(() => tcs.TrySetCanceled());
        
		ThreadPool.RegisterWaitForSingleObject(
			are,
			(state, timedOut) => ((TaskCompletionSource<bool>)state!).TrySetResult(!timedOut),
			tcs,
			-1,
			true);

		return tcs.Task.ContinueWith(t =>
		{
			registration.Dispose();
			return t;
		}, cancellationToken).Unwrap();
	}
}