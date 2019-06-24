namespace GlobalPollenProject.Identity.Email

type EmailMessage = {
    To: string
    Subject: string
    MessageHtml: string
}

type SendEmail = EmailMessage -> Async<System.Net.HttpStatusCode>

module Cloud =

    open SendGrid
    open SendGrid.Helpers.Mail
    open System.Text.RegularExpressions

    let sendAsync (sendGridApiKey:string) fromName fromEmail contents =
        let message = SendGridMessage()
        message.AddTo(EmailAddress(contents.To))
        message.From <- EmailAddress(fromEmail, fromName)
        message.Subject <- contents.Subject
        message.PlainTextContent <- Regex.Replace(contents.MessageHtml, "<[^>]*>", "")
        message.HtmlContent <- contents.MessageHtml
        let client = SendGridClient(sendGridApiKey)
        async {
            let! code = client.SendEmailAsync(message) |> Async.AwaitTask
            return code.StatusCode
        }
