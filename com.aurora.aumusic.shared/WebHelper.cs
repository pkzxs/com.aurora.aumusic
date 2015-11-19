﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace com.aurora.aumusic.shared
{
    public static class WebHelper
    {
        public async static Task<T> WebGETAsync<T>(string url, int timeout, T data)
        {
            WebRequest wrGETURL;
            wrGETURL = WebRequest.Create(url);
            wrGETURL.Method = "GET";
            Stream objStream;
            objStream = (await wrGETURL.GetResponseAsync()).GetResponseStream();

            StreamReader objReader = new StreamReader(objStream);

            string sLine = "";
            sLine = await objReader.ReadToEndAsync();
            return JsonHelper.FromJson<T>(sLine);
        }
    }
}