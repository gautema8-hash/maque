using System.Collections.Generic;
using System.Threading.Tasks;
using CefSharp;

namespace MaoDuoDuo;

public class MyFileDialogHandler : IDialogHandler
{
	private readonly List<string> filePaths;

	public MyFileDialogHandler(List<string> filePaths)
	{
		this.filePaths = filePaths;
	}

	public bool OnFileDialog(IWebBrowser chromiumWebBrowser, IBrowser browser, CefFileDialogMode mode, string title, string defaultFilePath, List<string> acceptFilters, IFileDialogCallback callback)
	{
		TaskCompletionSource<FileDialogResult> taskCompletionSource = new TaskCompletionSource<FileDialogResult>();
		FileDialogResult result = new FileDialogResult
		{
			FilePaths = filePaths
		};
		taskCompletionSource.SetResult(result);
		callback.Continue(filePaths);
		return true;
	}
}
