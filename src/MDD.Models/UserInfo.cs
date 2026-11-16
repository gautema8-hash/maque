using Newtonsoft.Json;

namespace MDD.Models;

public class UserInfo
{
	public string Username { get; set; }

	public bool Vip { get; set; }

	[JsonProperty("expiration_date")]
	public string ExpirationDate { get; set; }

	[JsonProperty("local_time")]
	public string LocalTime { get; set; }

	public string Token { get; set; }

	[JsonProperty("renewal_link")]
	public string RenewalLink { get; set; }

	[JsonProperty("is_pay")]
	public bool IsPay { get; set; }

	[JsonProperty("wechat_img")]
	public string WechatImg { get; set; }

	[JsonProperty("member_description")]
	public string MemberDescription { get; set; }
}
