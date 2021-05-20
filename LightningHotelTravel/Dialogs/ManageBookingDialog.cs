using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using LightningHotelTravel.Helpers;
using LightningHotelTravel.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace LightningHotelTravel.Dialogs
{
    public class ManageBookingDialog : CancelAndHelpDialog
    {
        private readonly string ReferenceNumberTextPrompt = "RefNumberTxtPrompt";
        private readonly string BookingActionsTextPrompt = "BookingActionsTxtPrompt";
        private const string BookingContactTextPrompt = "BookingContactTextPrompt";

        public string referenceNumber { get; set; }
        public ManageBooking hotelBooking { get; set; }

        public ManageBookingDialog() : base(nameof(ManageBookingDialog))
        {
            AddDialog(new TextPrompt(ReferenceNumberTextPrompt));
            AddDialog(new TextPrompt(BookingActionsTextPrompt));
            AddDialog(new TextPrompt(BookingContactTextPrompt));

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                GetReferenceNumberStepAsync,
                BookingActionStepAsync,
                BookingActionActStepAsync,
                FinalStepAsync
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> GetReferenceNumberStepAsync(WaterfallStepContext stepcontext,
            CancellationToken cancellationtoken)
        {
            var promptOptions = new PromptOptions()
            {
                Prompt = MessageFactory.Text("Enter your Reference Number")
            };

            return await stepcontext.PromptAsync(ReferenceNumberTextPrompt, promptOptions, cancellationtoken);
        }

        private async Task<DialogTurnResult> BookingActionStepAsync(WaterfallStepContext stepcontext,
            CancellationToken cancellationtoken)
        {
            referenceNumber = stepcontext.Result.ToString();

            hotelBooking = await GetHotelBooking(referenceNumber);
            if (hotelBooking is null)
            {
                await stepcontext.Context.SendActivityAsync(MessageFactory.Text("Invalid Reference Number"),
                    cancellationtoken);
                return await stepcontext.EndDialogAsync(null, cancellationtoken);
            }

            var adaptiveCard = new AdaptiveCard(new AdaptiveSchemaVersion(1, 0))
            {
                Body = new List<AdaptiveElement>()
                {
                    new AdaptiveTextBlock()
                    {
                        Text = $"Your Booking for {hotelBooking.hotel.name}",
                        Wrap = true,
                        Size = AdaptiveTextSize.Large,
                        Weight = AdaptiveTextWeight.Bolder
                    },
                    new AdaptiveFactSet()
                    {
                        Facts = new List<AdaptiveFact>()
                        {
                            new AdaptiveFact("Duration of Stay",
                                $"{hotelBooking.start} - {hotelBooking.end}"),
                            new AdaptiveFact("Room Count", $"{hotelBooking.roomCount}"),
                            new AdaptiveFact("Notes", $"{hotelBooking.notes}"),
                        }
                    },
                    new AdaptiveTextBlock("Booked by:")
                    {
                        Wrap = true,
                        Weight = AdaptiveTextWeight.Bolder,
                        Size = AdaptiveTextSize.Medium
                    },
                    new AdaptiveFactSet()
                    {
                        Facts = new List<AdaptiveFact>()
                        {
                            new AdaptiveFact("Name",
                                $"{hotelBooking.bookingContact.FirstName} {hotelBooking.bookingContact.LastName}"),
                            new AdaptiveFact("Email", $"{hotelBooking.bookingContact.Email}")
                        }
                    }
                }
            };

            var response = (Activity)MessageFactory.Attachment(new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = adaptiveCard
            });

            await stepcontext.Context.SendActivityAsync(response, cancellationtoken);

            
            var updateBookingActionsCard =
                new AdaptiveCardPicker().CreateAdaptiveCardAttachment(Card.UpdateBookingActionsCard);

            var response1 = MessageFactory.Attachment(updateBookingActionsCard);

            await stepcontext.Context.SendActivityAsync(response1, cancellationtoken);

            var promptOptions = new PromptOptions()
            {
                Prompt = MessageFactory.Text("")
            };

            return await stepcontext.PromptAsync(BookingActionsTextPrompt, promptOptions, cancellationtoken);
        }
        
        private async Task<DialogTurnResult> BookingActionActStepAsync(WaterfallStepContext stepcontext,
            CancellationToken cancellationtoken)
        {
            var optionsAct = stepcontext.Result.ToString();
            switch (optionsAct)
            {
                case "Edit Contact":
                    var bookingContactCard = new AdaptiveCardPicker().CreateAdaptiveCardAttachment(Card.BookingContact);
                    var response = MessageFactory.Attachment(bookingContactCard);
                    var promptOptions = new PromptOptions()
                    {
                        Prompt = (Activity)response
                    };

                    var res = await stepcontext.PromptAsync(BookingContactTextPrompt, promptOptions, cancellationtoken);
                    return res;

                case "Edit Hotel":
                    await stepcontext.Context.SendActivityAsync(
                        MessageFactory.Text("To update hotel details delete booking and begin a new booking"),
                        cancellationtoken);
                    return await stepcontext.EndDialogAsync(null, cancellationtoken);

                case "Cancel Booking":
                    var isSuccess = await CancelBookingAsync(referenceNumber);
                    if (isSuccess)
                    {
                        await stepcontext.Context.SendActivityAsync(
                            MessageFactory.Text("Booking Deleted Successfully"), cancellationtoken);
                    }
                    else
                    {
                        await stepcontext.Context.SendActivityAsync(
                            MessageFactory.Text("An Error Occurred please try again"), cancellationtoken);
                    }

                    return await stepcontext.EndDialogAsync(null, cancellationtoken);

                default:
                    await stepcontext.Context.SendActivityAsync(MessageFactory.Text("Invalid Operation"),
                        cancellationtoken);
                    return await stepcontext.EndDialogAsync(null, cancellationtoken);
            }


        }


        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepcontext,
            CancellationToken cancellationtoken)
        {
            var bookingContact =
                JsonConvert.DeserializeObject<Bookingcontact>((string)stepcontext.Result);

            var bookingDetails = new HotelBookingDetails()
            {
                id=hotelBooking.id,
                HotelId = hotelBooking.hotelId,
                BookingContact = bookingContact,
                End = hotelBooking.end,
                Notes = hotelBooking.notes,
                RoomCount = hotelBooking.roomCount,
                Start = hotelBooking.start
            };

            var json = JsonConvert.SerializeObject(bookingDetails, Formatting.Indented);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var client = new HttpClient();
            var response = await client.PutAsync("https://palbina-bot-api.azurewebsites.net/booking", content,
                cancellationtoken);

            if (response.IsSuccessStatusCode)
            {
                var activity = MessageFactory.Text("Booking update Successful!");
                await stepcontext.Context.SendActivityAsync(activity, cancellationtoken);
            }
            else
            {
                var activity = MessageFactory.Text("An error occurred");
                await stepcontext.Context.SendActivityAsync(activity, cancellationtoken);
            }
            return await stepcontext.EndDialogAsync(bookingDetails, cancellationtoken);
        }

        private async Task<bool> CancelBookingAsync(string referenceNumber)
        {
            var client = new HttpClient();
            var response =
                await client.DeleteAsync($"https://palbina-bot-api.azurewebsites.net/booking/{referenceNumber}");
            return response.IsSuccessStatusCode;
        }

        private async Task<ManageBooking> GetHotelBooking(string referenceNumber)
        {
            var client = new HttpClient();

            var response = await client.GetAsync($"https://palbina-bot-api.azurewebsites.net/booking/{referenceNumber}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ManageBooking>(json);
            }
            else
            {
                return null;
            }
        }
    }
}
