﻿using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Tsubasa.Helper
{
    public class WebJson
    {
        /// <summary>
        ///     Gets JSON data from a remote url asyncronously
        /// </summary>
        /// <param name="url">the url to pull json data from</param>
        /// <param name="headers">headers to add to the request as a webheader collection</param>
        /// <returns>JSON formatted data from that URL in the form of a JObject</returns>
        public static async Task<JObject> RequestJsonAsync(string url, WebHeaderCollection headers = null)
        {
            //Create the web request using the URL
            var request = (HttpWebRequest) WebRequest.Create(url);
            request.Method = "GET"; //set the request method to get
            request.Headers = headers ?? new WebHeaderCollection();
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            string content;
            var response = await request.GetResponseAsync();
            await using (var stream = response.GetResponseStream())
            {
                using (var sr = new StreamReader(stream ?? throw new Exception("RequestJson stream reader was null")))
                {
                    content = await sr.ReadToEndAsync();
                }
            }

            return JObject.Parse(content);
        }
    }
}