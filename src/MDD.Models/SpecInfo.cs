using Newtonsoft.Json;

namespace MDD.Models;

public class SpecInfo
{
	[JsonProperty("spec_key")]
	public string SpecKey { get; set; }

	[JsonProperty("spec_value")]
	public string SpecValue { get; set; }
}
