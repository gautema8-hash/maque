using System.Collections.Generic;

namespace MDD.Models;

public class ProductGoodsProperty
{
	public string key { get; set; }

	public string ref_pid { get; set; }

	public string reference_id { get; set; }

	public List<string> values { get; set; }

	public int unit { get; set; } = -1;


	public string unitStr { get; set; } = "";


	public int type { get; set; } = 1;

}
