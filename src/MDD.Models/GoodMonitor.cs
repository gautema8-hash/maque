namespace MDD.Models.Good;

public class GoodMonitor
{
	public int Id { get; set; }

	public int Num { get; set; }

	public string GoodId { get; set; }

	public string GoodName { get; set; }

	public int GoodNum { get; set; }

	public int GoodState { get; set; } = 1;


	public string UpdateTime { get; set; }

	public string CreateTime { get; set; }
}
