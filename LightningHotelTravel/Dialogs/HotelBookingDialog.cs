using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using LightningHotelTravel.Helpers;
using LightningHotelTravel.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LightningHotelTravel.Dialogs
{
    public class HotelBookingDialog : CancelAndHelpDialog
    {
        private int count = 0;
        private List<Hotel> _hotels = new List<Hotel>();


        private const string CountryTextPrompt = "CountryTextPrompt";
        private const string HotelTextPrompt = "HotelTextPrompt";
        private const string StartDateTimePrompt = "StartDateTimePrompt";
        private const string EndDateTimePrompt = "EndDateTimePrompt";

        public HotelBookingDialog() : base(nameof(HotelBookingDialog))
        {
            var Conversation = new ConversationState(new MemoryStorage());
            var DlgState = Conversation.CreateProperty<DialogState>("DlgState");
            Dialogs = new DialogSet(DlgState);

            AddDialog(new TextPrompt(CountryTextPrompt));
            AddDialog(new TextPrompt(HotelTextPrompt));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new DateTimePrompt(StartDateTimePrompt));

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                CountryStepAsync,
                HotelStepAsync,
                TravelDateStartStepAsync,
                ConfirmStepAsync,
                FinalStepAsync
            }));
            
            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);

        }

        private async Task<DialogTurnResult> CountryStepAsync(WaterfallStepContext stepcontext, CancellationToken cancellationtoken)
        {
            var hotelBookingDetails = (HotelBookingDetails)stepcontext.Options;
            if (string.IsNullOrEmpty(hotelBookingDetails.HotelCountry))
            {
                var countriesCard = new AdaptiveCardPicker().CreateAdaptiveCardAttachment(Card.Countries);
                var response =
                    MessageFactory.Attachment(countriesCard, ssml: "What country would you like to travel to?");


                var promptOptions = new PromptOptions
                {
                    Prompt = (Activity)response,
                };
                var res=await stepcontext.PromptAsync(CountryTextPrompt, promptOptions, cancellationtoken);
                return res;
            }
            return await stepcontext.NextAsync(hotelBookingDetails.HotelCountry, cancellationtoken);

        }

        private async Task<DialogTurnResult> HotelStepAsync(WaterfallStepContext stepcontext, CancellationToken cancellationtoken)
        {
            var hotelBookingDetails = (HotelBookingDetails)stepcontext.Options;
            if (string.IsNullOrEmpty(hotelBookingDetails.HotelId))
            {
                var hotels = await FetchHotelsAsync(hotelBookingDetails.HotelCountry);
                hotels.Add("More");
                
                // Create card
                var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 0))
                {
                    Body = new List<AdaptiveElement>()
                    {
                        new AdaptiveTextBlock()
                        {
                            Text = "Select a hotel or load more",
                            Size = AdaptiveTextSize.Medium,
                            Weight = AdaptiveTextWeight.Bolder
                        }
                    },
                    // Use LINQ to turn the choices into submit actions
                    Actions = hotels.Select(hotel => new AdaptiveSubmitAction()
                    {
                        Title = hotel,
                        Data = hotel,  // This will be a string
                    }).ToList<AdaptiveAction>(),
                };

                var promptOptions = new PromptOptions
                {
                    Prompt = (Activity)MessageFactory.Attachment(new Attachment
                    {
                        ContentType = AdaptiveCard.ContentType,
                        // Convert the AdaptiveCard to a JObject
                        Content = JObject.FromObject(card),
                    }),
                    Choices = ChoiceFactory.ToChoices(hotels),
                    // Don't render the choices outside the card
                    Style = ListStyle.None,
                };

                var res = await stepcontext.PromptAsync(HotelTextPrompt, promptOptions, cancellationtoken);
                return res;
            }

            return await stepcontext.NextAsync(hotelBookingDetails.HotelCountry, cancellationtoken);
        }

        private async Task<DialogTurnResult> TravelDateStartStepAsync(WaterfallStepContext stepcontext, CancellationToken cancellationtoken)
        {
            var hotelBookingDetails = (HotelBookingDetails)stepcontext.Options;
            if (string.IsNullOrEmpty(hotelBookingDetails.Start))
            {
                var hotelCheckInDateCard = new AdaptiveCardPicker().CreateAdaptiveCardAttachment(Card.HotelCheckInDate);
                var response =
                    MessageFactory.Attachment(hotelCheckInDateCard, ssml: "When will you check into the Hotel?");


                var promptOptions = new PromptOptions
                {
                    Prompt = (Activity)response,
                };

                var res = await stepcontext.PromptAsync(StartDateTimePrompt, promptOptions, cancellationtoken);
                return res;

            }
            return await stepcontext.NextAsync(hotelBookingDetails.HotelCountry, cancellationtoken);
        }

        private Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepcontext, CancellationToken cancellationtoken)
        {
            throw new NotImplementedException();
        }

        private Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepcontext, CancellationToken cancellationtoken)
        {
            throw new NotImplementedException();
        }

        private async Task<List<string>> FetchHotelsAsync(string countryName)
        {
            var client = new HttpClient()
            {
                BaseAddress =
                    new Uri(
                        $"https://palbina-bot-api.azurewebsites.net/hotel?take=5&skip={count}&searchString={countryName}&orderBy=asc")
            };

            var response = await client.GetAsync("");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var hotels = JsonConvert.DeserializeObject<List<Hotel>>(content);
                _hotels.AddRange(hotels);
                count += hotels.Count;
                return hotels.Count == 0
                    ? new List<string> { "No more Hotels found in the selected country" }
                    : hotels.Select(h => h.name).ToList();
            }

            return new List<string>() { "None" };
        }
    }

}
