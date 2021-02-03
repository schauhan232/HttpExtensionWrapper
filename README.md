# HttpExtensionWrapper

<p> HttpExtensionWrapper is class libarary that will help to serializ/deserilaze your request/response classes. With best practices mentioned by popular experts. </p>

---------------
Example  of To use for HttpGet
   * `_httpClient.Get<YourResponseClass>("CallingApiEndPointAddress")`
   * `_httpClient.Get<YourRequestClass, YourResponseClass>("CallingApiEndPointAddress", instanceOfYourRequestClass)`

Example of HttpPut
   * `_httpClient.Put<YourRequestClass>("CallingApiEndPointAddress"", instanceOfYourRequestClass)`

There's support of Get,Post, Put and Delete. Please comment if you want to extend it

---------------

# Inspired From Blogs
* https://johnthiriet.com/efficient-api-calls/
* https://www.stevejgordon.co.uk/using-httpcompletionoption-responseheadersread-to-improve-httpclient-performance-dotnet
