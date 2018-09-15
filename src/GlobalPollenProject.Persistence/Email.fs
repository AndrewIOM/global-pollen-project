module EmailSender

module Cloud =

    open SendGrid
    open SendGrid.Helpers.Mail
    open System.Text.RegularExpressions

    let send (sendGridApiKey:string) fromName fromEmail correspondingEmail subject messageHtml =
        let message = SendGridMessage()
        message.AddTo(EmailAddress(correspondingEmail))
        message.From <- EmailAddress(fromEmail, fromName)
        message.Subject <- subject
        message.PlainTextContent <- Regex.Replace(messageHtml, "<[^>]*>", "")
        message.HtmlContent <- messageHtml
        let client = SendGridClient(sendGridApiKey)
        async {
            let! code = client.SendEmailAsync(message) |> Async.AwaitTask
            return code
        }
