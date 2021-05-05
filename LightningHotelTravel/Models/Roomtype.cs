using System;

public class Roomtype
{
    public string id { get; set; }
    public string name { get; set; }
    public string description { get; set; }
    public int maxOccupancy { get; set; }
    public string hotelId { get; set; }
    public DateTime createdAt { get; set; }
    public DateTime updatedAt { get; set; }
}