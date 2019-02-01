using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ImageHost.Utils
{
    public class Tinify
    {
        private readonly string TinifyApiUrl = "https://api.tinify.com";
        private readonly HttpClient client;
        
        public Tinify (string ApiKey)
        {
            if (ApiKey == null) throw new ArgumentNullException();
            client = new HttpClient { BaseAddress = new Uri(TinifyApiUrl) };

            #region Authorization Header
            var encodedToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"api:{ApiKey}"));
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", encodedToken);
            #endregion
        }

        async public Task<bool> ValidateKey ()
        {
            // Same as official sdk
            var response = await client.PostAsync("/shrink", null);
            var code = Convert.ToInt16(response.StatusCode);
            if (code >= 400 && code < 500 && code != 401)
            {
                return true;
            }

            return false;
        }

        async public Task<byte[]> Compress (byte[] image)
        {
            HttpResponseMessage response;
            try
            {
                response = await client.PostAsync("/shrink", new ByteArrayContent(image));
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"[Tinify] Push / Network Error: {e.ToString()}");
                throw e;
            }

            if (response.StatusCode != System.Net.HttpStatusCode.Created)
            {
                await ParseErrorResponse(response);
                throw new Exception("Compress failed");
            }

            byte[] compressedImage;
            try
            {
                var imageUrl = response.Headers.GetValues("Location").ToArray()[0];
                response = await client.GetAsync(imageUrl);
                if (response.IsSuccessStatusCode)
                {
                    compressedImage = await response.Content.ReadAsByteArrayAsync();
                } else
                {
                    await ParseErrorResponse(response);
                    throw new Exception("Image download failed");
                }
            } catch (HttpRequestException e) // Response with code 201 should have at least one 'Location' header.
            {
                Console.WriteLine($"[Tinify] Pull / Network Error: ${e.ToString()}");
                throw new Exception("Failed to download compressed image");
            }

            return compressedImage;
        }

        async public Task<uint> GetCompressionCount ()
        {
            var response = await client.PostAsync("/shrink", null);
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                return Convert.ToUInt16(response.Headers.GetValues("Compression-Count").ToArray()[0]);
            } else
            {
                await ParseErrorResponse(response);
                throw new Exception("Failed to query compression count");
            }
        }

        #region Helper
        async private Task ParseErrorResponse (HttpResponseMessage response)
        {
            Console.WriteLine($"Unexpected response from Tinify API, Code: ${response.StatusCode.ToString()}");
            Console.WriteLine($"Response Body: ${await response.Content.ReadAsStringAsync()}");
        }
        #endregion
    }
}
