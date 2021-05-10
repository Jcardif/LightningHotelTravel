using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace LightningHotelTravel.Helpers
{
    public class AdaptiveCardPicker
    {
        /// <summary>
        ///     Load attachment from embedded resource.
        /// </summary>
        /// <returns></returns>
        public Attachment CreateAdaptiveCardAttachment(Card card)
        {
            string cardResourcePath;
            ;
            switch (card)
            {
                case Card.Welcome:
                    cardResourcePath = GetType().Assembly.GetManifestResourceNames()
                        .First(name => name.EndsWith("welcomeCard.json"));
                    break;

                case Card.Countries:
                    cardResourcePath = GetType().Assembly.GetManifestResourceNames()
                        .First(name => name.EndsWith("countriesCard.json"));
                    break;

                case Card.HotelCheckInDate:
                    cardResourcePath = GetType().Assembly.GetManifestResourceNames()
                        .First(name => name.EndsWith("hotelCheckInDateCard.json"));
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(card), card, null);
            }

            using var stream = GetType().Assembly.GetManifestResourceStream(cardResourcePath);
            using var reader = new StreamReader(stream);
            var adaptiveCard = reader.ReadToEnd();
            return new Attachment
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(adaptiveCard)
            };
        }
    }

    public enum Card
    {
        Welcome, Countries, HotelCheckInDate
    }
}
