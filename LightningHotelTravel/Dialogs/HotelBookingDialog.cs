using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.ExceptionServices;
using System.Text;
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
        private Dictionary<string, int> count = new Dictionary<string, int>()
        {
            {"Netherlands", 0},
            {"Italy", 0},
            {"Germany", 0},
            {"Spain", 0},
            {"Greece", 0},
            {"Morocco", 0},
            {"Austria", 0},
            {"France", 0},
            {"Great Britain", 0}
        };
        private List<Hotel> _hotels = new List<Hotel>();


        private const string CountryTextPrompt = "CountryTextPrompt";
        private const string HotelTextPrompt = "HotelTextPrompt";
        private const string StartDateTextPrompt = "StartDateTimePrompt";
        private const string EndDateTimePrompt = "EndDateTimePrompt";
        private const string BookingContactTextPrompt = "BookingContactTextPrompt";
        private const string ConfirmTextPrompt = "ConfirmTextPropmpt";
        private const string RoomCountTextPrompt = "RoomCountTextPrompt";

        public HotelBookingDialog() : base(nameof(HotelBookingDialog))
        {
            var Conversation = new ConversationState(new MemoryStorage());
            var DlgState = Conversation.CreateProperty<DialogState>("DlgState");
            Dialogs = new DialogSet(DlgState);

            AddDialog(new TextPrompt(CountryTextPrompt));
            AddDialog(new TextPrompt(HotelTextPrompt));
            AddDialog(new TextPrompt(ConfirmTextPrompt));
            AddDialog(new TextPrompt(StartDateTextPrompt));
            AddDialog(new TextPrompt(EndDateTimePrompt));
            AddDialog(new TextPrompt(RoomCountTextPrompt));
            AddDialog(new TextPrompt(BookingContactTextPrompt));

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                CountryStepAsync,
                HotelStepAsync,
                TravelDateStartStepAsync,
                TravelDateEndStepAsync,
                RoomCountStepAsync,
                UserDetailsFormStepAsync,
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
            hotelBookingDetails.HotelCountry = (string) stepcontext.Result;
            if (string.IsNullOrEmpty(hotelBookingDetails.HotelName))
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

            return await stepcontext.NextAsync(hotelBookingDetails.HotelName, cancellationtoken);
        }


        private async Task<DialogTurnResult> TravelDateStartStepAsync(WaterfallStepContext stepcontext, CancellationToken cancellationtoken)
        {
            var hotelBookingDetails = (HotelBookingDetails)stepcontext.Options;
            hotelBookingDetails.HotelName = (string)stepcontext.Result;

            var selectedHotel = _hotels.First(h => h.name == hotelBookingDetails.HotelName);
            hotelBookingDetails.HotelId = selectedHotel.id;

            var images = selectedHotel.images.Take(4).Select(i => new AdaptiveImage(i.url)
            {
                Size = AdaptiveImageSize.Stretch,
                Spacing = AdaptiveSpacing.Padding
            }).ToList();

            var hotelInfo = new AdaptiveCard(new AdaptiveSchemaVersion(1, 0))
            {
                Body = new List<AdaptiveElement>()
                {
                    new AdaptiveTextBlock()
                    {
                        Text = $"Selected Hotel",
                        Size = AdaptiveTextSize.Large,
                        Weight = AdaptiveTextWeight.Bolder
                    },
                    new AdaptiveTextBlock()
                    {
                        Size = AdaptiveTextSize.Medium,
                        Text =
                            $"{selectedHotel.name}\r\r{selectedHotel.address.line1} - {selectedHotel.address.postalCode} \r\r{selectedHotel.address.city} - {selectedHotel.address.countryName}",
                        Weight = AdaptiveTextWeight.Bolder
                    },
                    new AdaptiveTextBlock()
                    {
                        Text = selectedHotel.description,
                        Wrap = true
                    },
                    new AdaptiveImageSet()
                    {
                        Images = images,
                    },
                    new AdaptiveFactSet()
                    {
                        Facts = new List<AdaptiveFact>()
                        {
                            new AdaptiveFact("Rating", selectedHotel.rating.ToString()),
                            new AdaptiveFact("No. of available Rooms", selectedHotel.roomCount.ToString()),
                        }
                    }
                }
            };
            var resp = (Activity)MessageFactory.Attachment(new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = hotelInfo
            });

            await stepcontext.Context.SendActivityAsync(resp, cancellationtoken);


            if (hotelBookingDetails.Start is null)
            {
                
                var checkInCard= new AdaptiveCardPicker().CreateAdaptiveCardAttachment(Card.HotelCheckInDate);

                var response =
                    MessageFactory.Attachment(checkInCard, ssml: "When will you check into the Hotel?");

                var promptOptions = new PromptOptions
                {
                    Prompt = (Activity)response,
                };

                var res = await stepcontext.PromptAsync(StartDateTextPrompt, promptOptions, cancellationtoken);
                return res;
            }

            return await stepcontext.NextAsync(hotelBookingDetails.Start, cancellationtoken);
        }

        private async Task<DialogTurnResult> TravelDateEndStepAsync(WaterfallStepContext stepcontext, CancellationToken cancellationtoken)
        {
            var hotelBookingDetails = (HotelBookingDetails)stepcontext.Options;
            hotelBookingDetails.Start = DateTime.Parse(stepcontext.Result.ToString());

            if (hotelBookingDetails.End is null)
            {
                var checkoutCard = new AdaptiveCardPicker().CreateAdaptiveCardAttachment(Card.HotelCheckOutDate);

                var response =
                    MessageFactory.Attachment(checkoutCard, ssml: "When will you check out of the Hotel?");


                var promptOptions = new PromptOptions
                {
                    Prompt = (Activity)response,
                };

                var res = await stepcontext.PromptAsync(EndDateTimePrompt, promptOptions, cancellationtoken);
                return res;
            }

            return await stepcontext.NextAsync(hotelBookingDetails.End, cancellationtoken);
        }

        private async Task<DialogTurnResult> RoomCountStepAsync(WaterfallStepContext stepcontext,
            CancellationToken cancellationtoken)
        {
            var hotelBookingDetails = (HotelBookingDetails)stepcontext.Options;
            hotelBookingDetails.End = DateTime.Parse(stepcontext.Result.ToString());

            if (hotelBookingDetails.RoomCount == 0)
            {
                var selectedHotel = _hotels.FirstOrDefault(h => h.name == hotelBookingDetails.HotelName);
                
                var promptOptions = new PromptOptions()
                {
                    Prompt = MessageFactory.Text($"How many rooms are you booking? Rooms available are {selectedHotel.roomCount}")
                };

                return await stepcontext.PromptAsync(RoomCountTextPrompt, promptOptions, cancellationtoken);
            }

            return await stepcontext.NextAsync(hotelBookingDetails.RoomCount, cancellationtoken);
        }


        private async Task<DialogTurnResult> UserDetailsFormStepAsync(WaterfallStepContext stepcontext,
            CancellationToken cancellationtoken)
        {
            var hotelBookingDetails = (HotelBookingDetails)stepcontext.Options;
            hotelBookingDetails.RoomCount = Convert.ToInt32(stepcontext.Result);

            if (hotelBookingDetails.BookingContact is null)
            {
                var bookingContactCard = new AdaptiveCardPicker().CreateAdaptiveCardAttachment(Card.BookingContact);
                var response = MessageFactory.Attachment(bookingContactCard);
                var promptOptions = new PromptOptions()
                {
                    Prompt = (Activity)response
                };

                var res = await stepcontext.PromptAsync(BookingContactTextPrompt, promptOptions, cancellationtoken);
                return res;
            }

            return await stepcontext.NextAsync(hotelBookingDetails.BookingContact, cancellationtoken);

        }


        private async Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepcontext, CancellationToken cancellationtoken)
        {
            var hotelBookingDetails = (HotelBookingDetails)stepcontext.Options;
            var bookingContact =
                JsonConvert.DeserializeObject<Bookingcontact>((string)stepcontext.Result);

            hotelBookingDetails.BookingContact = bookingContact;
            var selectedHotel = _hotels.FirstOrDefault(h => h.name == hotelBookingDetails.HotelName);

            var confirmCard = new AdaptiveCard(new AdaptiveSchemaVersion(1, 0))
            {
                Body = new List<AdaptiveElement>()
                {
                    new AdaptiveTextBlock()
                    {
                        Text = "Confirm your details:",
                        Wrap = true,
                        Size = AdaptiveTextSize.Large,
                        Weight = AdaptiveTextWeight.Bolder
                    },
                    new AdaptiveColumnSet()
                    {
                        Columns = new List<AdaptiveColumn>()
                        {
                            new AdaptiveColumn()
                            {
                                Width = "25",
                                Items = new List<AdaptiveElement>
                                {
                                    new AdaptiveImage(selectedHotel?.images.First().url)
                                }
                            },
                            new AdaptiveColumn()
                            {
                                Width = "50",
                                Items = new List<AdaptiveElement>()
                                {
                                    new AdaptiveTextBlock()
                                    {
                                        Text = hotelBookingDetails.HotelName,
                                        Wrap = true,
                                        Size = AdaptiveTextSize.Medium,
                                        Weight = AdaptiveTextWeight.Bolder
                                    },
                                    new AdaptiveTextBlock()
                                    {
                                        Text = $"{selectedHotel?.address.city}, {selectedHotel?.address.country}",
                                        Wrap = true
                                    }
                                }
                            }
                        }
                    },
                    new AdaptiveFactSet()
                    {
                        Facts = new List<AdaptiveFact>()
                        {
                            new AdaptiveFact("Duration of Stay",
                                $"{hotelBookingDetails.Start.Value.ToLongDateString()} - {hotelBookingDetails.End.Value.ToLongDateString()}"),
                            new AdaptiveFact("Room Count", $"{hotelBookingDetails.RoomCount}"),
                            new AdaptiveFact("Notes", $"{hotelBookingDetails.Notes}"),
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
                                $"{hotelBookingDetails.BookingContact.FirstName} {hotelBookingDetails.BookingContact.LastName}"),
                            new AdaptiveFact("Email", $"{hotelBookingDetails.BookingContact.Email}")
                        }
                    },
                    new AdaptiveColumnSet()
                    {
                        Columns = new List<AdaptiveColumn>()
                        {
                            new AdaptiveColumn()
                            {
                                Width = "stretch",
                                Items = new List<AdaptiveElement>()
                                {
                                    new AdaptiveActionSet()
                                    {
                                        Actions = new List<AdaptiveAction>()
                                        {
                                            new AdaptiveSubmitAction()
                                            {
                                                Title = "Cancel",
                                                Data = "Cancel",
                                                Style = "destructive"
                                            }
                                        }
                                    }
                                }
                            },
                            new AdaptiveColumn()
                            {
                                Width = "stretch",
                                Items = new List<AdaptiveElement>()
                                {
                                    new AdaptiveActionSet()
                                    {
                                        Actions = new List<AdaptiveAction>()
                                        {
                                            new AdaptiveSubmitAction()
                                            {
                                                Title = "Confirm",
                                                Data = "Confirm",
                                                Style = "positive"
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            var response = (Activity)MessageFactory.Attachment(new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = confirmCard
            });

            await stepcontext.Context.SendActivityAsync(response, cancellationtoken);

            var promptOptions = new PromptOptions()
            {
                Prompt = MessageFactory.Text("")
            };
            

            return await stepcontext.PromptAsync(ConfirmTextPrompt, promptOptions, cancellationtoken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepcontext, CancellationToken cancellationtoken)
        {
            var bookingDetails = (HotelBookingDetails)stepcontext.Options;
            if (stepcontext.Result.ToString() != "Confirm")
                return await stepcontext.EndDialogAsync(bookingDetails, cancellationtoken);

            var client = new HttpClient();
            var json = JsonConvert.SerializeObject(bookingDetails, Formatting.Indented);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://palbina-bot-api.azurewebsites.net/booking", content,
                cancellationtoken);
            if (response.IsSuccessStatusCode)
            {
                var activity = MessageFactory.Text("Booking Successful!");
                await stepcontext.Context.SendActivityAsync(activity, cancellationtoken);
            }
            else
            {
                var activity = MessageFactory.Text("An error occurred");
                await stepcontext.Context.SendActivityAsync(activity, cancellationtoken);
            }
            return await stepcontext.EndDialogAsync(bookingDetails, cancellationtoken);

        }

        private async Task<List<string>> FetchHotelsAsync(string countryName)
        {
            var countryCount = count.FirstOrDefault(c => c.Key == countryName).Value;
            var client = new HttpClient()
            {
                BaseAddress =
                    new Uri(
                        $"https://palbina-bot-api.azurewebsites.net/hotel?take=5&skip={countryCount}&searchString={countryName}&orderBy=asc")
            };

            var response = await client.GetAsync("");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var hotels = JsonConvert.DeserializeObject<List<Hotel>>(content);
                _hotels.AddRange(hotels);
                count.Remove(countryName);
                count.Add(countryName, countryCount + 1);
                return hotels.Count == 0
                    ? new List<string> { "No more Hotels found in the selected country" }
                    : hotels.Select(h => h.name).ToList();
            }

            return new List<string>() { "None" };
        }
    }

}
