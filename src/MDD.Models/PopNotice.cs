using System.Collections.Generic;

namespace MDD.Models;

public class PopNotice
{
	public double Width { get; set; }

	public double Height { get; set; }

	public List<NoticeInfo> NoticeInfo { get; set; } = new List<NoticeInfo>();

}
