using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Net.Sockets;

class Program
{
    static readonly string _smtpServer = "smtp.office365.com";
    static readonly int _port = 587;
    public static string? _senderEmail = "";
    public static string? _receiverEmail = "";
    public static string? _password = "";
    public static string? myLoopTimeOut = "";

    static async Task Main(string[] args)
    {
        try
        {
            TimeSpan LoopTimeout;
            Stopwatch stopwatch = new Stopwatch();
            string? websiteUrl = "";
            string? _statusCode = "";
            double myLoopTimeOutDouble;
            int timeoutSeconds = 0;

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddUserSecrets<Program>() // This line adds the secrets.json
                .Build();

            //secrets.json
            _senderEmail = configuration["SenderEmail"];
            _receiverEmail = configuration["ReceiverEmail"];
            _password = configuration["Password"];

            LogToFile($"Sender={_senderEmail} Receiver={_receiverEmail}", true);

            var websitesData = configuration.GetSection("WebsitesConfig").Get<WebsitesData>();
            if (websitesData == null)
            {
                // Handle the case when websitesData is null
                Console.WriteLine("WebsitesConfig section is missing in the configuration.");
                return;
            }

            _receiverEmail = configuration["ReceiverEmail"];
            if (string.IsNullOrEmpty(_receiverEmail))
            {
                // Handle the case when _receiverEmail is null or empty
                Console.WriteLine("Receiver Email is missing in the configuration.");
                return;
            }

            _senderEmail = configuration["SenderEmail"];
            if (string.IsNullOrEmpty(_senderEmail))
            {
                // Handle the case when _senderEmail is null or empty
                Console.WriteLine("Sender Email is missing in the configuration.");
                return;
            }

            _password = configuration["Password"];
            if (string.IsNullOrEmpty(_password))
            {
                // Handle the case when _password is null or empty
                Console.WriteLine("Password is missing in the configuration.");
                return;
            }

            myLoopTimeOut = configuration["LoopTimeoutSeconds"];
            if (string.IsNullOrEmpty(myLoopTimeOut))
            {
                // Handle the case when myLoopTimeOut is null or empty
                Console.WriteLine("LoopTimeoutSeconds is missing in the configuration.");
                return;
            }

            if (Double.TryParse(myLoopTimeOut, out myLoopTimeOutDouble))
            {
                LoopTimeout = TimeSpan.FromSeconds(myLoopTimeOutDouble);
            }
            else
            {
                LoopTimeout = TimeSpan.FromSeconds(900);
            }

            while (true)
            {
                if (websitesData?.Websites != null)
                {
                    foreach (var websiteInfo in websitesData.Websites)
                    {
                        using var client = new HttpClient();
                        websiteUrl = websiteInfo.Url;
                        timeoutSeconds = websiteInfo.TimeoutSeconds;
                        client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

                        try
                        {
                            stopwatch.Start();
                            HttpResponseMessage response = await client.GetAsync(websiteUrl).ConfigureAwait(false);
                            if (response.StatusCode == HttpStatusCode.MovedPermanently)
                            {
                                // Handle the status code 301 (Moved Permanently) do nothing
                            }
                            else
                            {
                                response.EnsureSuccessStatusCode();
                            }
                            string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                            _statusCode = response.StatusCode.ToString();
                            stopwatch.Stop();
                            // Get the elapsed time as a TimeSpan value.
                            TimeSpan ts = stopwatch.Elapsed;
                            // Write the response to the console
                            if (response.StatusCode == System.Net.HttpStatusCode.OK)
                            {
                                Console.WriteLine($"GET request to {websiteUrl} succeeded. Response: {responseBody}");
                                Console.WriteLine($"GET request to {websiteUrl} succeeded. Response: {response.StatusCode}");
                                LogToFile($"GET request to {websiteUrl} succeeded. Response: {response.StatusCode}", false);
                            }
                            else
                            {
                                Console.WriteLine($"GET request to {websiteUrl} succeeded. Response: {response.StatusCode}");
                                LogToFile($"GET request to {websiteUrl} succeeded. Response: {response.StatusCode}", false);
                            }
                            // Format and display the TimeSpan value.
                            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                                ts.Hours, ts.Minutes, ts.Seconds,
                                ts.Milliseconds / 10);
                            Console.WriteLine("HTTP Get Response Time:" + elapsedTime);
                            LogToFile("HTTP Get Response Time:" + elapsedTime, true);
                            // Reset stopwatch.
                            stopwatch.Reset();
                        }
                        catch (HttpRequestException e)
                        {
                            Console.WriteLine($"Exception caught for website {websiteUrl}: {e.Message}");
                            // Send an email and log the error to a file
                            await SendEmail(_receiverEmail, $"Exception caught for website {websiteUrl}", e.Message);
                            LogErrorToFile(e);
                        }
                        catch (TaskCanceledException e)
                        {
                            Console.WriteLine($"Request to website {websiteUrl} timed out: {e.Message}");
                            // Send an email and log the error to a file
                            await SendEmail(_receiverEmail, $"Request to website {websiteUrl} timed out", e.Message);
                            LogErrorToFile(e);
                        }
                    }
                }
                else
                {
                    break;
                }
                // Wait for the specified loop timeout before the next round of requests
                await Task.Delay(LoopTimeout);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            LogErrorToFile(ex);
        }
    }

    static async Task SendEmail(string recipient, string subject, string body)
    {
        if (string.IsNullOrEmpty(_senderEmail))
        {
            // Handle the case when _senderEmail is null or empty
            Console.WriteLine("Email is missing in the configuration.");
            return;
        }
        using (SmtpClient server = new SmtpClient(_smtpServer, _port))
        {
            server.EnableSsl = true;
            server.Credentials = new NetworkCredential(_senderEmail, _password);
            server.UseDefaultCredentials = false;

            MailMessage mail = new MailMessage();
            mail.From = new MailAddress(_senderEmail);
            mail.To.Add(recipient);
            mail.Subject = subject;
            mail.Body = body;
            mail.IsBodyHtml = true;

            await server.SendMailAsync(mail);
        }
    }

    // Method to log the error to a file
    static void LogErrorToFile(Exception e)
    {
        // Specify your log file path here
        string directoryPath = ".";
        string fileName = "Errorlog_" + DateTime.Now.ToString("yyyyMMdd") + ".txt";
        string filePath = Path.Combine(directoryPath, fileName);
        using (StreamWriter writer = new StreamWriter(filePath, true))
        {
            writer.WriteLine($"Time: {DateTime.Now}");
            writer.WriteLine(e.ToString());
            writer.WriteLine($"Stack Trace: {e.StackTrace}");
            writer.WriteLine("=================================");
        }
    }

    // Method to log the information to a file
    static void LogToFile(string msg, bool setTime)
    {
        // Specify your log file path here
        string directoryPath = ".";
        string fileName = "Infolog_" + DateTime.Now.ToString("yyyyMMdd") + ".txt";
        string filePath = Path.Combine(directoryPath, fileName);

        using (StreamWriter writer = new StreamWriter(filePath, true))
        {
            if (setTime)
                writer.WriteLine($"Time: {DateTime.Now}");
            writer.WriteLine(msg.ToString());
            writer.WriteLine("=================================");
        }
    }
}

// Define the data structure for deserializing the JSON
class WebsitesData
{
    public WebsiteInfo[]? Websites { get; set; }
}

class WebsiteInfo
{
    public string? Url { get; set; }
    public int TimeoutSeconds { get; set; }
}
