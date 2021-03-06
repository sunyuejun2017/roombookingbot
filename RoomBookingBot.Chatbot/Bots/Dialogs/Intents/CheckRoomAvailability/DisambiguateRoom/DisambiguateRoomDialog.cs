﻿using System.Collections.Generic;
using System.Linq;
using JamesMann.BotFramework.Middleware;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Prompts.Choices;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.DateTime;
using RoomBookingBot.Chatbot.Bots.DialogStateWrappers;
using RoomBookingBot.Chatbot.Extensions;
using RoomBookingBot.Chatbot.Model;
using RoomBookingBot.Extensions;

namespace RoomBookingBot.Chatbot.Bots.Dialogs.Intents.CheckRoomAvailability.DisambiguateRoom
{
    public class DisambiguateRoomDialog : DialogContainer
    {
        public static DisambiguateRoomDialog Instance = new DisambiguateRoomDialog();

        private DisambiguateRoomDialog() : base(Id)
        {
            var recognizer = new DateTimeRecognizer(Culture.English);
            var model = recognizer.GetDateTimeModel();

            Dialogs.Add(Id, new WaterfallStep[]
            {
                async (dc, args, next) =>
                {
                    var stateWrapper = new DisambiguateRoomDialogStateWrapper(dc.ActiveDialog.State) {Booking = (BookingRequest) args["bookingRequest"]};
                    var bookingRequest = stateWrapper.Booking;

                    bookingRequest.AvailableRooms = (await MicrosoftGraphExtensions.GetMicrosoftGraphFindMeetingRooms(
                        dc.Context.Services.Get<ConversationAuthToken>(AzureAdAuthMiddleware.AUTH_TOKEN_KEY).AccessToken)).ToArray();

                    if (string.IsNullOrEmpty(bookingRequest.Room))
                    {
                        var roomChoices = new List<Choice> {new Choice {Value = "No preference"}};
                        roomChoices.AddRange(from room in bookingRequest.AvailableRooms select new Choice {Value = room.DisplayName});

                        await dc.Prompt("choicePrompt", "Do you have a preference which room?", new ChoicePromptOptions
                        {
                            Choices = roomChoices
                        }).ConfigureAwait(false);
                    }
                    else
                    {
                        await dc.End();
                    }
                },
                async (dc, args, next) =>
                {
                    await dc.End(new Dictionary<string, object>
                    {
                        ["Value"] = ((FoundChoice) args["Value"]).Value
                    });
                }
            });

            Dialogs.Add("choicePrompt", new ChoicePrompt("en"));
        }

        public static string Id => "disambiguateRoomDialog";
    }
}