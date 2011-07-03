﻿using System;
using System.Collections.Generic;
using System.Net;

#if SILVERLIGHT
using System.Windows;
#endif

namespace LinqToTwitter
{
    public abstract class OAuthAuthorizer
    {
        public OAuthAuthorizer()
        {
            OAuthRequestTokenUrl = "https://api.twitter.com/oauth/request_token";
            OAuthAuthorizeUrl = "https://api.twitter.com/oauth/authorize";
            OAuthAccessTokenUrl = "https://api.twitter.com/oauth/access_token";

            OAuthTwitter = new OAuthTwitter();

#if SILVERLIGHT
            if (!Application.Current.IsRunningOutOfBrowser)
            {
                ProxyUrl =
                        Application.Current.Host.Source.Scheme + "://" +
                        Application.Current.Host.Source.Host + ":" +
                        Application.Current.Host.Source.Port + "/LinqToTwitterProxy.ashx?url="; 
            }
#else
            ProxyUrl = string.Empty;
#endif
        }

        /// <summary>
        /// URL for OAuth Request Tokens
        /// </summary>
        public string OAuthRequestTokenUrl { get; set; }

        /// <summary>
        /// URL for OAuth authorization
        /// </summary>
        public string OAuthAuthorizeUrl { get; set; }

        /// <summary>
        /// URL for OAuth Access Tokens
        /// </summary>
        public string OAuthAccessTokenUrl { get; set; }

        /// <summary>
        /// URL for Silverlight proxy
        /// </summary>
        public string ProxyUrl
        {
            get
            {
                return OAuthTwitter.ProxyUrl;
            }
            set
            {
                OAuthTwitter.ProxyUrl = value;
            }
        }

        /// <summary>
        /// Contains general OAuth functionality
        /// </summary>
        public IOAuthTwitter OAuthTwitter { get; set; }

        private IOAuthCredentials m_credentials;

        /// <summary>
        /// Holds ConsumerKey, ConsumerSecret, and AccessToken
        /// 
        /// Note: Populate Credentials before setting this property
        /// </summary>
        public IOAuthCredentials Credentials
        {
            get
            {
                return m_credentials;
            }
            set
            {
                m_credentials = value;
                OAuthTwitter.OAuthConsumerKey = value.ConsumerKey;
                OAuthTwitter.OAuthConsumerSecret = value.ConsumerSecret;
                OAuthTwitter.OAuthToken = value.OAuthToken;
                OAuthTwitter.OAuthTokenSecret = value.AccessToken;
            }
        }

        public bool IsAuthorized
        {
            get
            {
                if (Credentials == null)
                {
                    throw new ArgumentNullException("Credentials", "You must set the Credentials property.");
                }

                return
                    !string.IsNullOrEmpty(Credentials.ConsumerKey) &&
                    !string.IsNullOrEmpty(Credentials.ConsumerSecret) &&
                    !string.IsNullOrEmpty(Credentials.OAuthToken) &&
                    !string.IsNullOrEmpty(Credentials.AccessToken);
            }
        }

        public string UserId { get; set; }

        public string ScreenName { get; set; }

        public TimeSpan ReadWriteTimeout { get; set; }

        public TimeSpan Timeout { get; set; }

        public string UserAgent { get; set; }

        public bool UseCompression { get; set; }

        /// <summary>
        /// Initializes the request in ways common to GET and POST requests.
        /// </summary>
        /// <param name="webRequest">The request to initialize.</param>
        protected void InitializeRequest(WebRequest webRequest)
        {
#if !SILVERLIGHT
            var request = webRequest as HttpWebRequest;

            request.UserAgent = UserAgent;

            if (this.ReadWriteTimeout > TimeSpan.Zero)
            {
                request.ReadWriteTimeout = (int)ReadWriteTimeout.TotalMilliseconds;
            }

            if (this.Timeout > TimeSpan.Zero)
            {
                request.Timeout = (int)Timeout.TotalMilliseconds;
            }

            if (this.UseCompression)
            {
                request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip, deflate");
                request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            }
#endif
        }

        /// <summary>
        /// OAuth Get
        /// </summary>
        /// <param name="url">Twitter Query</param>
        /// <returns>Request to be sent to Twitter</returns>
        public WebRequest Get(Request request)
        {
            string outUrl;
            string queryString;
            OAuthTwitter.GetOAuthQueryString(HttpMethod.GET, request, string.Empty, out outUrl, out queryString);

#if SILVERLIGHT
            var fullUrl = ProxyUrl + request.FullUrl;

            var req = HttpWebRequest.Create(fullUrl);
            req.Headers[HttpRequestHeader.Authorization] = new OAuthTwitter().PrepareAuthHeader(queryString);
#else
            var req = HttpWebRequest.Create(request.FullUrl) as HttpWebRequest;
            req.Headers[HttpRequestHeader.Authorization] = new OAuthTwitter().PrepareAuthHeader(queryString);

            InitializeRequest(req);
#endif

            return req;
        }

        /// <summary>
        /// OAuth Post
        /// </summary>
        /// <param name="request">The request with the endpoint URL and the parameters to 
        /// include in the POST entity.  Must not be null.</param>
        /// <returns>request to send</returns>
        public virtual HttpWebRequest PostRequest(Request request, IDictionary<string, string> postData)
        {
            var auth = OAuthTwitter.GetOAuthQueryStringForPost(request, postData);

#if SILVERLIGHT
            var req = HttpWebRequest.Create(
                ProxyUrl + request.Endpoint + 
                (string.IsNullOrEmpty(ProxyUrl) ? "?" : "&") +
                request.QueryString) as HttpWebRequest;
#else
            var req = HttpWebRequest.Create(request.FullUrl) as HttpWebRequest;
#endif
            req.Method = HttpMethod.POST.ToString();
            req.Headers[HttpRequestHeader.Authorization] = auth;
            req.ContentLength = 0;

            InitializeRequest(req);

            return req;
        }
        /// <summary>
        /// OAuth Post
        /// </summary>
        /// <param name="request">The request with the endpoint URL and the parameters to 
        /// include in the POST entity.  Must not be null.</param>
        /// <returns>request to send</returns>
        public virtual HttpWebResponse Post(Request request, IDictionary<string, string> postData)
        {
            var req = PostRequest(request, postData);
            return Utilities.AsyncGetResponse(req);
        }

        /// <summary>
        /// Async OAuth Post
        /// </summary>
        /// <param name="request">The request with the endpoint URL and the parameters to 
        /// include in the POST entity.  Must not be null.</param>
        /// <returns>HttpWebRequest for post</returns>
        public virtual HttpWebRequest PostAsync(Request request, IDictionary<string, string> postData)
        {
            var auth = OAuthTwitter.GetOAuthQueryStringForPost(request, postData);

            var req = WebRequest.Create(
                    ProxyUrl + request.Endpoint +
                    (string.IsNullOrEmpty(ProxyUrl) ? "?" : "&") +
                    request.QueryString)
                as HttpWebRequest;
            req.Method = HttpMethod.POST.ToString();
            req.Headers[HttpRequestHeader.Authorization] = auth;
            req.ContentLength = 0;

            InitializeRequest(req);

            return req;
        }
    }
}