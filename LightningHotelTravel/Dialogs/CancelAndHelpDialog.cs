// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio CoreBot v4.13.1

using System.Threading;
using System.Threading.Tasks;
using LightningHotelTravel.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace LightningHotelTravel.Dialogs
{
    public class CancelAndHelpDialog : ComponentDialog
    {
        private const string HelpMsgText = "Show help here";
        private const string CancelMsgText = "Cancelling...";

        public CancelAndHelpDialog(string id)
            : base(id)
        {
        }

        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext innerDc,
            CancellationToken cancellationToken = default)
        {
            var result = await InterruptAsync(innerDc, cancellationToken);
            if (result != null) return result;

            return await base.OnContinueDialogAsync(innerDc, cancellationToken);
        }

        private async Task<DialogTurnResult> InterruptAsync(DialogContext innerDc, CancellationToken cancellationToken)
        {
            if (innerDc.Context.Activity.Type == ActivityTypes.Message)
            {
                if (!string.IsNullOrEmpty(innerDc.Context.Activity.Text))
                {
                    var text = innerDc.Context.Activity.Text.ToLowerInvariant();

                    switch (text)
                    {
                        case "help":
                        case "?":
                            var helpMessage = MessageFactory.Text(HelpMsgText, HelpMsgText, InputHints.ExpectingInput);
                            await innerDc.Context.SendActivityAsync(helpMessage, cancellationToken);
                            return new DialogTurnResult(DialogTurnStatus.Waiting);

                        case "cancel":
                        case "quit":
                            var cancelMessage = MessageFactory.Text(CancelMsgText, CancelMsgText, InputHints.IgnoringInput);
                            await innerDc.Context.SendActivityAsync(cancelMessage, cancellationToken);
                            return await innerDc.CancelAllDialogsAsync(cancellationToken);
                    }
                }

                if (innerDc.Context.Activity.Value != null &&
                    innerDc.Context.Activity.Value.ToString().Contains("date"))
                {
                    var date = JsonConvert.DeserializeObject<InputDate>(innerDc.Context.Activity.Value.ToString());
                    innerDc.Context.Activity.Text = $"{date.Date} {date.Time}";
                    return await innerDc.ContinueDialogAsync(cancellationToken);
                }

                if (innerDc.Context.Activity.Value != null &&
                    innerDc.Context.Activity.Value.ToString().Contains("BookingContactCard"))
                {
                    innerDc.Context.Activity.Text = innerDc.Context.Activity.Value.ToString();
                    return await innerDc.ContinueDialogAsync(cancellationToken);
                }

            }

            return null;
        }
    }

    public class InputDate
    {
        public string Date { get; set; }
        public string Time { get; set; }
    }
}