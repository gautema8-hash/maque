namespace MDD.Common;

public class DownloadClass
{
	public event DownLoadCallback downCallback;

	public void DoSomethingAsync(long maxNum, long readLen)
	{
		if (this.downCallback != null)
		{
			this.downCallback(maxNum, readLen);
		}
	}

	private string ProcessFile(long maxNum)
	{
		return "完成任务！";
	}
}
