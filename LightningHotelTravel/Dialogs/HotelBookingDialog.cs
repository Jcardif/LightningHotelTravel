using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LightningHotelTravel.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace LightningHotelTravel.Dialogs
{
    public class HotelBookingDialog : CancelAndHelpDialog
    {
        private int count = 0;
        private List<Hotel> _hotels = new List<Hotel>();

        private const string CountryChoicePrompt = "CountryChoicePrompt";
        private const string HotelChoicePrompt = "HotelChoicePrompt";

        public HotelBookingDialog() : base(nameof(HotelBookingDialog))
        {
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new DateResolverDialog());
            AddDialog(new ChoicePrompt(HotelChoicePrompt));
            AddDialog(new ChoicePrompt(CountryChoicePrompt));
            

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                CountryStepAsync,
                HotelStepAsync,
                TravelDateStepAsync,
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
                var countries = new List<Choice>()
                {
                    new() {Value = "Netherlands", Synonyms = new List<string> {"NE"}},
                    new() {Value = "Italy", Synonyms = new List<string> {"NE"}},
                    new() {Value = "Germany", Synonyms = new List<string> {"NE"}},
                    new() {Value = "Spain", Synonyms = new List<string> {"NE"}},
                    new() {Value = "Greece", Synonyms = new List<string> {"NE"}},
                    new() {Value = "Morocco", Synonyms = new List<string> {"NE"}},
                    new() {Value = "Austria", Synonyms = new List<string> {"NE"}},
                    new() {Value = "France", Synonyms = new List<string> {"NE"}},
                };

                var textPrompt = stepcontext.Context.Activity.CreateReply(
                    "What country would you like to travel to?\nThe following countries are currently supported.");
                var promptOptions = new PromptOptions
                {
                    Prompt = textPrompt,
                    Choices = countries
                };
                return await stepcontext.PromptAsync(CountryChoicePrompt, promptOptions, cancellationtoken);
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
                var hotelNames = hotels.Select(hotel => new Choice { Value = hotel }).ToList();
                var textPrompt = stepcontext.Context.Activity.CreateReply(
                    "Select a hotel or load more");
                var promptOptions = new PromptOptions
                {
                    Prompt = textPrompt,
                    Choices = hotelNames
                };
                var res = await stepcontext.PromptAsync(HotelChoicePrompt, promptOptions, cancellationtoken);
                if (res.Status == DialogTurnStatus.Complete)
                {
                    var t= (HotelBookingDetails)stepcontext.Options;
                }
                return res;
            }

            return await stepcontext.NextAsync(hotelBookingDetails.HotelCountry, cancellationtoken);
        }

        private Task<DialogTurnResult> TravelDateStepAsync(WaterfallStepContext stepcontext, CancellationToken cancellationtoken)
        {
            throw new NotImplementedException();
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
                var hotels = JsonConvert.DeserializeObject<ReturnHotel>(content);
                _hotels.AddRange(hotels.hotels);
                count += hotels.hotels.Count;
                return hotels.hotels.Count == 0
                    ? new List<string> { "No more Hotels found in the selected country" }
                    : hotels.hotels.Select(h => h.name).ToList();
            }

            return new List<string>() { "None" };
        }
    }


    public class ReturnHotel
    {
        public List<Hotel> hotels { get; set; }
    }

}
