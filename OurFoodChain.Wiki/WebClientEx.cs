﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Wiki {

    public class WebClientEx :
        WebClient {

        public CookieContainer CookieContainer { get; set; } = new CookieContainer();

        protected override WebRequest GetWebRequest(Uri uri) {

            WebRequest request = base.GetWebRequest(uri);

            if (request is HttpWebRequest)
                (request as HttpWebRequest).CookieContainer = CookieContainer;

            return request;
        }
    }

}