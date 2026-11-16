namespace MDD.Models.Video;

public class Variables
{
	public string keyword { get; set; }

	public string pcursor { get; set; }

	public string page
	{
		get
		{
			return "search";
		}
		set
		{
			page = value;
		}
	}
}
