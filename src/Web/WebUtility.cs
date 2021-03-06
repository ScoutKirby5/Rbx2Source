﻿#pragma warning disable 0649

using System;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace Rbx2Source.Web
{
    struct WebApiError
    {
        public int Code;
        public string Message;
    }

    struct CdnPender
    {
        public string Url;
        public bool Final;
    }

    class WebUtility
    {
        private static byte[] ReadFullStream(Stream stream, bool close = true)
        {
            MemoryStream streamBuffer = new MemoryStream();
            int count = 1;
            while (stream.CanRead && count > 0)
            {
                byte[] buffer = new byte[1024];
                count = stream.Read(buffer, 0, 1024);
                streamBuffer.Write(buffer, 0, count);
            }

            byte[] result = streamBuffer.ToArray();
            streamBuffer.Close();
            if (close) stream.Close();

            return result;
        }

        private static void wait(float time)
        {
            int ms = (int)(time * 1000);
            Task waitTask = Task.Delay(ms);
            waitTask.Wait();
        }

        public static byte[] DownloadData(string url)
        {
            HttpWebRequest request = WebRequest.CreateHttp(url);
            request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip");
            request.UserAgent = "Roblox";
            request.Proxy = null;
            request.UseDefaultCredentials = true;
            request.Method = "GET";
            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            Stream responseStream = response.GetResponseStream();
            byte[] result = null;
            if (response.ContentEncoding == "gzip")
            { 
                GZipStream decompressor = new GZipStream(responseStream, CompressionMode.Decompress);
                result = ReadFullStream(decompressor);
            }
            else
            {
                result = ReadFullStream(responseStream);
            }
            return result;
        }

        public static string DownloadString(string url)
        {
            byte[] data = DownloadData(url);
            return Encoding.UTF8.GetString(data);
        }

        public static Bitmap DownloadImage(string url)
        {
            byte[] data = DownloadData(url);
            using (Stream imgStream = new MemoryStream(data))
            {
                return new Bitmap(imgStream);
            }
        }

        public static T DownloadJSON<T>(string url)
        {
            byte[] content = DownloadData(url);
            string json = Encoding.UTF8.GetString(content);
            Console.WriteLine(json);
            return JsonConvert.DeserializeObject<T>(json);
        }

        public static T DownloadRbxApiJSON<T>(string subUrl, string apiServer = "api")
        {
            string url = "https://" + apiServer + ".roblox.com/" + subUrl;
            return DownloadJSON<T>(url);
        }

        public static string PendCdnUrl(string url, bool log = true)
        {
            string result = null;
            bool final = false;
            string dots = "..";
            while (!final)
            {
                CdnPender pender = DownloadJSON<CdnPender>(url);
                result = pender.Url;
                final = pender.Final;
                if (!final)
                {
                    dots += ".";
                    if (log)
                        Rbx2Source.Print("Waiting for finalization of " + url + dots);

                    wait(1f);

                    if (dots.Length > 13)
                        throw new Exception("CdnPender timed out after 10 retries! Roblox's servers may be overloaded right now.\nTry again after a few minutes!");
                }
            }
            return result;
        }

        public static string ResolveHashUrl(string hash)
        {
            int comp = 31;
            foreach (char c in hash)
                comp ^= (byte)c;

            int id = comp % 8;
            return "https://t" + id + ".rbxcdn.com/" + hash;
        }
    }
}
