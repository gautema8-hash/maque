using System.Collections.Generic;

namespace MDD.Models;

public class ReviewData
{
	public string comment { get; set; }

	public string stars { get; set; }

	public string desc_score { get; set; }

	public string logistics_score { get; set; }

	public string service_score { get; set; }

	public string comprehensive_dsr { get; set; }

	public string specs { get; set; }

	public string time { get; set; }

	public string avatar { get; set; }

	public string name { get; set; }

	public ReviewAppend append { get; set; }

	public string append_time_text { get; set; }

	public List<ReviewPicture> pictures { get; set; }

	public ReviewVideo video { get; set; }

	public string review_id { get; set; }

	public string sku_id { get; set; }

	public string friend_tag { get; set; }

	public string friend_tag_url { get; set; }

	public string order_num_text { get; set; }

	public string main_type { get; set; }

	public string anonymous { get; set; }

	public string enable_anonymous_operate { get; set; }

	public string review_operate_status { get; set; }

	public List<string> medal_list { get; set; }

	public string favor_count { get; set; }

	public string is_favored { get; set; }

	public string reply_count { get; set; }

	public List<ReviewReply> reply_list { get; set; }
}
