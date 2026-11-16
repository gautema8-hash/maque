using Newtonsoft.Json;

namespace MDD.Models;

public class JsonResponse
{
	[JsonProperty("success")]
	public bool Success { get; set; }

	[JsonProperty("error_code")]
	public int ErrorCode { get; set; }

	[JsonProperty("result")]
	public Result Result { get; set; }
}
