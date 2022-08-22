using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeechServices
{
    class Translate
    {
        public static string TranslateText(string lng, string text, string key)
        {
            string host = "https://api.cognitive.microsofttranslator.com";
            string route = "/translate?api-version=3.0&to=" + lng;
            string subscriptionKey = key;

            System.Object[] body = new System.Object[] { new { Text = text } };
            var requestBody = JsonConvert.SerializeObject(body);

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(host + route);
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                request.Headers.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
                request.Headers.Add("Ocp-Apim-Subscription-Region", "eastus");
                var response = client.SendAsync(request).Result;
                var jsonResponse = JArray.Parse(response.Content.ReadAsStringAsync().Result);
                var result = JObject.Parse(jsonResponse[0].ToString())["translations"].First()["text"].ToString();

                return result;
            }
        }
    }
}
