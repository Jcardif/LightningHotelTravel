using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LightningHotelTravel.Models
{
    public class ManageBooking
    {
        public string id { get; set; }
        public string status { get; set; }
        public DateTime start { get; set; }
        public DateTime end { get; set; }
        public string referenceNumber { get; set; }
        public string userId { get; set; }
        public string notes { get; set; }
        public int roomCount { get; set; }
        public DateTime createdAt { get; set; }
        public DateTime updatedAt { get; set; }
        public string hotelId { get; set; }
        public Bookingcontact bookingContact { get; set; }
        public Hotel hotel { get; set; }
    }


    public partial class Bookingcontact
    {
        public string id { get; set; }
        public DateTime createdAt { get; set; }
        public DateTime updatedAt { get; set; }
    }

}
