using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;

namespace AzureBlobStorageForeach
{
    public class Utils
    {
        public static string RemoveLeadingGuids(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                throw new ArgumentException("Input string is null or empty.");
            }

            // Define a regex pattern to match two GUIDs separated by underscores at the beginning of the input
            string pattern = @"^([a-fA-F\d]{8}(-[a-fA-F\d]{4}){3}-[a-fA-F\d]{12}_)*";

            // Remove leading GUIDs using regex pattern
            string result = Regex.Replace(input, pattern, "");

            return result;
        }

        public static string GenerateContentDispositionHeader(string filename, bool isAttachment)
        {
            if (string.IsNullOrEmpty (filename))
            {
                throw new ArgumentException($"{nameof(filename)} is empty");
            }

            // Encode the filename in UTF-8
            string utf8Filename = EncodeUtf8Filename(filename);

            // Create a new ContentDispositionHeaderValue and set the filename attributes
            if (isAttachment)
            {
                var contentDisposition = new ContentDispositionHeaderValue("attachment");
                contentDisposition.Parameters.Add(new NameValueHeaderValue("filename", $"\"{ReplaceNonAsciiChars(filename)}\""));
                contentDisposition.Parameters.Add(new NameValueHeaderValue("filename*", utf8Filename));
                return contentDisposition.ToString();
            }
            return string.Empty;
        }

        static string EncodeUtf8Filename(string filename)
        {
            // URL-encode the filename using UTF-8 encoding
            byte[] utf8Bytes = Encoding.UTF8.GetBytes(filename);
            string encodedFilename = Uri.EscapeDataString(Encoding.UTF8.GetString(utf8Bytes));

            // Prefix with UTF-8'' as required by the Content-Disposition header
            return "UTF-8''" + encodedFilename;
        }

        static string ReplaceNonAsciiChars(string input)
        {
            var sb = new StringBuilder();
            foreach (var ch in input)
            {
                //printable Ascii range
                if (ch >= 32 && ch < 127)
                    sb.Append(ch);
                else if (ch < 32)
                    sb.Append(" ");
                else if (ch >= 127)
                    sb.Append("?");
            }

            return sb.ToString();
        }

        public static string SanitizeJsonString(string input)
        {
            // Replace non breaking white space with regular space
            return input.Replace('\u00A0', ' ');
        }
    }
}
