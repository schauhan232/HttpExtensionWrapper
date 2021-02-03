using Newtonsoft.Json;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Formatting = Newtonsoft.Json.Formatting;

namespace HttpWrapper
{
    public static class StreamExtensions
    {
        public static Task<string> StreamToStringAsync(this Stream stream)
        {
            if (stream == null)
                return null;

            var sr = new StreamReader(stream);

            return sr.ReadToEndAsync();
        }

        public static void SerializeJsonIntoStream<T>(this MemoryStream stream, T value)
        {
            using var sw = new StreamWriter(stream, new UTF8Encoding(false), 1024, true);
            using var jtw = new JsonTextWriter(sw) { Formatting = Formatting.None };
            var js = new JsonSerializer();
            js.Serialize(jtw, value);
            jtw.Flush();
        }

        public static T DeserializeJsonFromStream<T>(this Stream stream)
        {
            if (stream == null || stream.CanRead == false)
                return default;

            using var sr = new StreamReader(stream);
            using var jtr = new JsonTextReader(sr);
            var js = new JsonSerializer();
            var searchResult = js.Deserialize<T>(jtr);
            return searchResult;
        }
    }
}
