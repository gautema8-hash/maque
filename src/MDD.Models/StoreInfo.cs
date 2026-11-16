namespace MDD.Models;

public class StoreInfo
{
	public int Id { get; set; }

	public int Num { get; set; }

	public string UUID { get; set; }

	public string ShopId { get; set; }

	public string Mobile { get; set; }

	public string Name { get; set; }

	public string Status { get; set; }

	public string Staple { get; set; }

	public int ChooseState { get; set; }

	public bool IsEditable { get; set; } = true;


	public string CreateTime { get; set; }
}
