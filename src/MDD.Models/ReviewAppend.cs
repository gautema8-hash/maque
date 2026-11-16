using System.Collections.Generic;

namespace MDD.Models;

public class ReviewAppend
{
	public string time { get; set; }

	public string comment { get; set; }

	public List<ReviewPicture> pictures { get; set; }

	public ReviewVideo video { get; set; }
}
