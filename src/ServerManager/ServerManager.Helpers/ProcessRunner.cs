using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServerManager.Helpers;

public static class ProcessRunner
{
	public static async Task<int> RunAsync(string fileName, string arguments, string workingDirectory, IProgress<string>? output = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		IProgress<string> output2 = output;
		ProcessStartInfo startInfo = new ProcessStartInfo(fileName, arguments)
		{
			CreateNoWindow = true,
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			WorkingDirectory = workingDirectory,
			StandardOutputEncoding = Encoding.UTF8,
			StandardErrorEncoding = Encoding.UTF8
		};
		using Process process = new Process
		{
			StartInfo = startInfo,
			EnableRaisingEvents = true
		};
		process.OutputDataReceived += delegate(object _, DataReceivedEventArgs e)
		{
			if (!string.IsNullOrEmpty(e.Data))
			{
				output2?.Report(e.Data);
			}
		};
		process.ErrorDataReceived += delegate(object _, DataReceivedEventArgs e)
		{
			if (!string.IsNullOrEmpty(e.Data))
			{
				output2?.Report(e.Data);
			}
		};
		process.Start();
		process.BeginOutputReadLine();
		process.BeginErrorReadLine();
		await process.WaitForExitAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		return process.ExitCode;
	}
}
