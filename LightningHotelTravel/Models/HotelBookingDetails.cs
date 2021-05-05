namespace LightningHotelTravel.Models
{
    public class HotelBookingDetails
    {
        public string HotelCountry { get; set; }
        public string Start { get; set; }
        public string End { get; set; }
        public string Notes { get; set; }
        public int RoomCount { get; set; }
        public string HotelId { get; set; }
        public Bookingcontact BookingContact { get; set; }
    }

    public class Bookingcontact
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
    }
}