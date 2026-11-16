namespace MDD.Models;

public class ReviewReply
{
	public string reply_id { get; set; }

	public string review_id { get; set; }

	public string content { get; set; }

	public ReviewReplyUserInfo user_info { get; set; }

	public string is_owner { get; set; }

	public string time { get; set; }

	public string time_text { get; set; }

	public string reply_type { get; set; }

	public ReviewReplyUserInfo parent_user_info { get; set; }

	public string parent_reply_id { get; set; }

	public string parent_is_owner { get; set; }

	public string is_merchant { get; set; }

	public string root_reply_id { get; set; }
}
