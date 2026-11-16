namespace MDD.Models;

public class ReviewVideo
{
	public string url { get; set; }

	public string transcoded_url { get; set; }

	public string width { get; set; }

	public string height { get; set; }

	public string duration { get; set; }

	public string size { get; set; }

	public string cover_image_url { get; set; }

	public string cover_image_width { get; set; }

	public string cover_image_height { get; set; }

	public string music_id { get; set; }

	public string song_id { get; set; }

	public ReviewMusicInfo music_info { get; set; }

	public string play_music { get; set; }

	public string position { get; set; }
}
