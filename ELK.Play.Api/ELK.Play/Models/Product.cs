namespace ELK.Play.Models;

public class Product
{
    public int Id { get; set; }

    public string Title { get; set; }

    public string Description { get; set; }

    public decimal Price { get; set; }

    public int Quantity { get; set; }

    public string Producer { get; set; }

    public string[] Tags { get; set; }
}