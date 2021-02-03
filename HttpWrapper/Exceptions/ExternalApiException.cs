using System;
using System.Net;

namespace HttpWrapper
{
    public class ExternalApiException : ApplicationException
    {
        public ExternalApiException(
            HttpStatusCode statusCode,
            string content)
        {
            StatusCode = statusCode;
            Content = content;
        }

        public HttpStatusCode StatusCode { get; set; }

        public string Content { get; set; }
    }
}
