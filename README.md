# Http Get Status

This is a simple console application that checks the status of a list of websites.

## Description

The application continuously loops through a list of websites and sends HTTP GET requests to each one. It logs the response status and the time it took to get the response. If an exception occurs during the request, it logs the error and sends an email notification.

## Features

- **Continuous Monitoring**: The application continuously checks the status of the websites.
- **Error Handling**: The application handles HttpRequestException and TaskCanceledException, logs the error, and sends an email notification.
- **Response Time Logging**: The application logs the time it took to get the response from each website.
- **Status Code Logging**: The application logs the status code of the response from each website.

## Usage

Review the appsettings.json for examples.

1. Populate the `websitesData.Websites` list with the websites you want to monitor. Each website should have an `Url` and a `TimeoutSeconds`.
2. Set the `_receiverEmail` to the email address where you want to receive error notifications.
3. Set the `_senderEmail` to the sender's email address.
4. Set the `_password` for the email address account.
5. Set the `LoopTimeout` to the amount of time you want the application to wait before the next round of requests.
6. Run the application.

## Note

This is a simple application for educational purposes. For production use, consider adding more robust error handling and logging features.
