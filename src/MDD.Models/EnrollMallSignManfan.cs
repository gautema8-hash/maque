using Newtonsoft.Json;

namespace MDD.Models;

public class EnrollMallSignManfan
{
	[JsonProperty("need_sign_manfan_protocol")]
	public bool NeedSignManfanProtocol { get; set; }
}
