using System;

public class Hotel
{
    public string id { get; set; }
    public string name { get; set; }
    public string description { get; set; }
    public int rating { get; set; }
    public string locationId { get; set; }
    public int roomCount { get; set; }
    public DateTime createdAt { get; set; }
    public DateTime updatedAt { get; set; }
    public Image[] images { get; set; }
    public Location location { get; set; }
    public Address address { get; set; }
    public Roomtype[] roomTypes { get; set; }
}