using System;

namespace LightningHotelTravel.Models
{
    public class HotelBookingDetails
    {
        public string id { get; set; }
        public string HotelCountry { get; set; }
        public DateTime? Start { get; set; }
        public DateTime? End { get; set; }
        public string Notes { get; set; }
        public int RoomCount { get; set; }
        public string HotelName { get; set; }
        public string HotelId { get; set; }
        public Bookingcontact BookingContact { get; set; }
    }

    public partial class Bookingcontact
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
    }

}