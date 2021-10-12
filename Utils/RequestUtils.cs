using GoProCSharpDev.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace GoProCSharpDev.Utils
{
    class WebRequestUtils
    {
        public static bool ValidateIPv4(string ipString)
        {
            if (string.IsNullOrWhiteSpace(ipString)) return false;
            string[] splitValues = ipString.Split('.');
            if (splitValues.Length != 4) return false;
            return splitValues.All(r => byte.TryParse(r, out byte tempForParsing));
        }

        #region Get

        public static string Get(string url, string outputPath = null, bool async = false)
        {
            try
            {
                if (async)
                {
                    Debug.WriteLine("Getting response async...");
                    try
                    {
                        HttpClient httpClient = new HttpClient();

                        // Get file bytes length before download
                        WebClient webClient = new WebClient();
                        webClient.OpenRead(url);
                        long bytesTotal = Convert.ToInt64(webClient.ResponseHeaders["Content-Length"]);
                        Debug.WriteLine("Total bytes: " + bytesTotal);

                        // Get headers
                        WebHeaderCollection headers = webClient.ResponseHeaders;
                        string responseHeaderText = "";
                        string contentType = "";
                        foreach (string headerKey in headers.AllKeys)
                        {
                            responseHeaderText += headerKey + ":" + headers.Get(headerKey) + "\r\n";
                            if (headerKey.Contains("Content-Type"))
                            {
                                contentType = headers.Get(headerKey);
                            }
                        }
                        webClient.Dispose();

                        // With file stream
                        Task<Stream> getStreamAsyncTask = httpClient.GetStreamAsync(url);
                        getStreamAsyncTask.Wait();

                        Stream stream = getStreamAsyncTask.Result;
                        Debug.WriteLine("Got stream");

                        // Save with a file
                        using (FileStream strmFile = new FileStream(outputPath, FileMode.OpenOrCreate, FileAccess.Write))
                        {
                            byte[] buffer = new byte[16 * 1024];
                            int bytesWrote = 0;
                            int bytesRead;
                            do
                            {
                                bytesRead = stream.Read(buffer, 0, 16 * 1024);
                                strmFile.Write(buffer, 0, bytesRead);
                                bytesWrote += bytesRead;
                                Debug.WriteLine("Write bytes: " + bytesWrote + " / " + bytesTotal + " (" + bytesWrote / (double)bytesTotal * 100 + "%)");
                            }
                            while (bytesRead > 0);
                            strmFile.Close();
                        }

                        stream.Close();
                        Debug.WriteLine("Saved");
                        return responseHeaderText + "Output file path:" + outputPath;
                    }
                    catch (HttpRequestException error)
                    {
                        string errorMessage = "Error sending API request: " + error.Message;
                        Debug.WriteLine(errorMessage);
                        return errorMessage;
                    }
                }
                else
                {
                    // Non async request
                    // Make request
                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                    req.Method = "GET";
                    req.Host = "localhost";

                    // Send request
                    using (HttpWebResponse webResponse = (HttpWebResponse)req.GetResponse())
                    {
                        // Get status code
                        string responseStatusCode = "Status code:" + webResponse.StatusCode.ToString() + "(" + (int)webResponse.StatusCode + ")\r\n";

                        // Get headers
                        WebHeaderCollection headers = webResponse.Headers;
                        string responseHeaderText = "";
                        string contentType = "";
                        foreach (var headerKey in headers.AllKeys)
                        {
                            responseHeaderText += headerKey + ":" + headers.Get(headerKey) + "\r\n";
                            if (headerKey.Contains("Content-Type"))
                            {
                                contentType = headers.Get(headerKey);
                            }
                        }

                        if (outputPath == null)
                        {
                            // Get body
                            // Json text response
                            using (StreamReader sr = new StreamReader(webResponse.GetResponseStream(), Encoding.UTF8))
                            {
                                string responseBodyText = sr.ReadToEnd().ToString();

                                // If content is Json, format the result
                                if (contentType.Equals("application/json"))
                                {
                                    dynamic parsedJson = JsonConvert.DeserializeObject(responseBodyText);
                                    responseBodyText = JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
                                }

                                // Show result
                                return responseStatusCode + responseHeaderText + responseBodyText;
                            }
                        }
                        else
                        {
                            // Get body
                            // Accept file stream response
                            if (contentType.Equals("application/octet-stream") || contentType.Equals("image/jpeg") || contentType.Equals("video/mp4"))
                            {
                                using (Stream responseStream = webResponse.GetResponseStream())
                                {
                                    using (FileStream strmFile = new FileStream(outputPath, FileMode.OpenOrCreate, FileAccess.Write))
                                    {
                                        byte[] buffer = new byte[16 * 1024];
                                        int bytesRead;
                                        do
                                        {
                                            bytesRead = responseStream.Read(buffer, 0, 16 * 1024);
                                            strmFile.Write(buffer, 0, bytesRead);
                                        }
                                        while (bytesRead > 0);
                                        strmFile.Close();
                                    }
                                }

                                // Show result
                                return responseStatusCode + responseHeaderText + outputPath;
                            }
                            else
                            {
                                // Show result
                                return responseStatusCode + responseHeaderText + "Unknown response type";
                            }
                        }
                    }
                }
            }
            catch (Exception error)
            {
                string errorMessage = "Error sending API request: " + error.Message;
                Debug.WriteLine(errorMessage);

                // Show result
                return "Failed: " + error.Message;
            }
        }

        #endregion Get

        #region Post

        // POST Request (Async)
        // If isFile == true then data has to be a file path
        // Else the data will be a json string
        public static async Task PostAsync(long watcherId, string url, List<QueryParam> queryParams, string data, bool showResult = false, bool isFile = false)
        {
            // Data or File
            if (isFile) LoggingUtils.Info("Post File: " + url + " Headers: " + queryParams, watcherId);
            else LoggingUtils.Info("Post Json Data: " + url + " Headers: " + queryParams, watcherId);

            // Make request
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Add headers
            try
            {
                foreach (QueryParam header in queryParams)
                {
                    httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
            }
            catch (Exception error)
            {
                string errorMessage = "Error setting headers : " + error.Message;
                LoggingUtils.Error("Error setting headers : " + errorMessage, watcherId);
                return;
            }

            try
            {
                HttpResponseMessage response = null;
                if (isFile)
                {

                    // Create http content
                    string filePath = data;
                    var fileStream = File.OpenRead(filePath);
                    HttpContent httpContent = new StreamContent(fileStream);
                    string headerValues = string.Format("form-data; name=\"{0}\"; filename=\"{1}\"", "file", Path.GetFileName(filePath));
                    byte[] headerValueBytes = Encoding.UTF8.GetBytes(headerValues);
                    var encodedHeaderValues = new StringBuilder();
                    foreach (byte b in headerValueBytes)
                    {
                        encodedHeaderValues.Append((char)b);
                    }
                    httpContent.Headers.Add("Content-Disposition", encodedHeaderValues.ToString());

                    httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                    // Create from-data
                    var formData = new MultipartFormDataContent { httpContent };

                    // Post it!
                    response = httpClient.PostAsync(url, formData).Result;
                }
                else
                {
                    // Create string content
                    string jsonData = data;
                    StringContent stringContent = new StringContent(jsonData);

                    // Post it!
                    response = httpClient.PostAsync(url, stringContent).Result;
                }

                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();  // Here it causes thread dead
                LoggingUtils.Info("Response Body: " + responseBody, watcherId);

                // Show result
                if (showResult) MessageBox.Show(responseBody, "Success");
            }
            catch (HttpRequestException error)
            {
                string errorMessage = "Error sending API request: " + error.Message;
                LoggingUtils.Error("Http Request Exception: " + errorMessage, watcherId);

                // Show result
                if (showResult) MessageBox.Show(errorMessage, "Http Request Error");
            }
        }

        // POST Request (Not Async)
        public static void Post(string url, string queryParams, List<QueryParam> queryHeaders)
        {
            Debug.WriteLine("Post: " + url + " Params: " + queryParams);
            try
            {
                // Make request
                byte[] bs = Encoding.ASCII.GetBytes(queryParams); // transfer to ASCII code
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.Method = "POST";
                req.ContentType = "application/x-www-form-urlencoded";
                req.ContentLength = bs.Length;

                foreach (QueryParam header in queryHeaders)
                {
                    req.Headers.Add(header.Key, header.Value);
                }

                using (Stream reqStream = req.GetRequestStream())
                {
                    reqStream.Write(bs, 0, bs.Length);
                }

                // Process response
                using (HttpWebResponse webResponse = (HttpWebResponse)req.GetResponse())
                {
                    // Get status code
                    string responseStatusCode = "Status code:" + webResponse.StatusCode.ToString() + "(" + (int)webResponse.StatusCode + ")\r\n";

                    // Get headers
                    WebHeaderCollection headers = webResponse.Headers;
                    string responseHeaderText = "";
                    foreach (var headerKey in headers.AllKeys)
                    {
                        responseHeaderText += headerKey + ":" + headers.Get(headerKey) + "\r\n";
                    }

                    // Get body
                    using (StreamReader sr = new StreamReader(webResponse.GetResponseStream(), Encoding.UTF8))
                    {
                        string responseBodyText = sr.ReadToEnd().ToString();

                        // Show result
                        MessageBox.Show(responseStatusCode + responseHeaderText + responseBodyText, "Success");
                    }
                }
            }
            catch (Exception error)
            {
                string errorMessage = "Error sending API request: " + error.Message;
                Debug.WriteLine(errorMessage);

                // Show result
                MessageBox.Show(error.Message, "Failed");
            }
        }

        #endregion Post
    }
}
