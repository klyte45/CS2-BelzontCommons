using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace Belzont.Utils
{
    public static class BlueSkyPoster
    {
        public static void Post(string user, string password, string xmlPath, string projectFile, int textKind)
        {
            // 1. Login
            var loginPayload = $"{{\"identifier\":\"{EscapeJson(user)}\",\"password\":\"{EscapeJson(password)}\"}}";
            var loginResponse = PostJson("https://bsky.social/xrpc/com.atproto.server.createSession", loginPayload);

            var did = ExtractJsonValue(loginResponse, "did");
            var accessJwt = ExtractJsonValue(loginResponse, "accessJwt");

            var (text, link, linkTitle, linkDescription, imageBase64) = textKind switch
            {
                0 => GetChangelog(xmlPath, Path.GetDirectoryName(projectFile)),
                _ => ("MEOW! TEST!", null, null, null, null)
            };

            var textEachPost = DoTextSplit(text, 280);

            string previousPostUri = null;
            string previousPostCid = null;
            string rootPostUri = null;
            string rootPostCid = null;

            for (int i = 0; i < textEachPost.Count; i++)
            {
                var post = textEachPost[i];
                Console.WriteLine($"Posting part {i + 1}/{textEachPost.Count}: {post}");

                // 2. Post
                var createdAt = DateTime.UtcNow.ToString("o");

                string postPayload;
                if (i == 0)
                {
                    // First post - no reply, with link embed if available
                    var embedSection = "";
                    if (!string.IsNullOrEmpty(link))
                    {
                        var thumbSection = "";

                        // Upload image if provided
                        if (!string.IsNullOrEmpty(imageBase64))
                        {
                            try
                            {
                                var imageBlob = UploadBlob(imageBase64, "image/png", accessJwt);
                                if (imageBlob != null)
                                {
                                    thumbSection =
                                        $",\"thumb\":{{\"$type\":\"blob\",\"ref\":{{\"$link\":\"{imageBlob.cid}\"}},\"mimeType\":\"{imageBlob.mimeType}\",\"size\":{imageBlob.size}}}";
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Failed to upload image: {ex.Message}");
                            }
                        }

                        embedSection =
                            $",\"embed\":{{\"$type\":\"app.bsky.embed.external\",\"external\":{{\"uri\":\"{EscapeJson(link)}\",\"title\":\"{EscapeJson(linkTitle)}\",\"description\":\"{EscapeJson(linkDescription)}\"{thumbSection}}}}}";
                    }

                    postPayload =
                        "{" +
                        $"\"repo\":\"{EscapeJson(did)}\"," +
                        "\"collection\":\"app.bsky.feed.post\"," +
                        "\"record\":{" +
                        "\"$type\":\"app.bsky.feed.post\"," +
                        $"\"text\":\"{EscapeJson(post)}\"," +
                        $"\"createdAt\":\"{EscapeJson(createdAt)}\"{embedSection}" +
                        "}" +
                        "}";
                }
                else
                {
                    // Subsequent posts - reply to previous
                    postPayload =
                        "{" +
                        $"\"repo\":\"{EscapeJson(did)}\"," +
                        "\"collection\":\"app.bsky.feed.post\"," +
                        "\"record\":{" +
                        "\"$type\":\"app.bsky.feed.post\"," +
                        $"\"text\":\"{EscapeJson(post)}\"," +
                        $"\"createdAt\":\"{EscapeJson(createdAt)}\"," +
                        "\"reply\":{" +
                        "\"root\":{" +
                        $"\"uri\":\"{EscapeJson(rootPostUri)}\"," +
                        $"\"cid\":\"{EscapeJson(rootPostCid)}\"" +
                        "}," +
                        "\"parent\":{" +
                        $"\"uri\":\"{EscapeJson(previousPostUri)}\"," +
                        $"\"cid\":\"{EscapeJson(previousPostCid)}\"" +
                        "}" +
                        "}" +
                        "}" +
                        "}";
                }

                var postResponse = PostJson("https://bsky.social/xrpc/com.atproto.repo.createRecord", postPayload,
                    accessJwt);
                Console.WriteLine(postPayload);
                Console.WriteLine(postResponse);

                // Extract URI and CID for next post in thread
                var uri = ExtractJsonValue(postResponse, "uri");
                var cid = ExtractJsonValue(postResponse, "cid");

                if (i == 0)
                {
                    // First post becomes the root
                    rootPostUri = uri;
                    rootPostCid = cid;
                }

                // Current post becomes the parent for next post
                previousPostUri = uri;
                previousPostCid = cid;

                // Add a small delay between posts to be respectful to the API
                if (i < textEachPost.Count - 1)
                {
                    System.Threading.Thread.Sleep(1000);
                }
            }
        }

        private static List<string> DoTextSplit(string text, int length)
        {
            if (text.Length <= length)
                return new List<string>() { text };

            var lines = text.Split(new[] { '\n' }, StringSplitOptions.None);
            var result = new List<string>();
            var currentBlock = new StringBuilder();

            for (var index = 0; index < lines.Length; index++)
            {
                var line = lines[index];
                var lineWithNewline = line + (index == lines.Length - 1 ? "" : "\n");

                // If the line itself is longer than the max length, we need to split it
                if (lineWithNewline.Length > length)
                {
                    // Add current block if it has content
                    if (currentBlock.Length > 0)
                    {
                        result.Add(currentBlock.ToString().Trim());
                        currentBlock.Clear();
                    }

                    // Split the long line into chunks
                    var lineText = line;
                    while (lineText.Length > length)
                    {
                        result.Add(lineText[..length]);
                        lineText = lineText[length..];
                    }

                    // Add remaining part if any
                    if (lineText.Length <= 0) continue;
                    currentBlock.Append(lineText);
                    if (line != lines.Last())
                        currentBlock.Append("\n");
                }
                else
                {
                    // Check if adding this line would exceed the limit
                    if (currentBlock.Length + lineWithNewline.Length > length)
                    {
                        // Save current block and start a new one
                        if (currentBlock.Length > 0)
                        {
                            result.Add(currentBlock.ToString().Trim());
                            currentBlock.Clear();
                        }
                    }

                    currentBlock.Append(lineWithNewline);
                }
            }

            // Add the last block if it has content
            if (currentBlock.Length > 0)
            {
                result.Add(currentBlock.ToString().Trim());
            }

            return result;
        }

        private static (string, string, string, string, string) GetChangelog(string xmlPath, string projectRoot)
        {
            if (!File.Exists(xmlPath))
            {
                throw new FileNotFoundException($"File {xmlPath} does not exist");
            }

            try
            {
                var xmlDoc = new System.Xml.XmlDocument();
                xmlDoc.Load(xmlPath);

                // Exemplo de XPath - ajuste conforme a estrutura do seu XML
                var versionNode = xmlDoc.SelectSingleNode("//ModVersion");
                var changelogNode = xmlDoc.SelectSingleNode("//ChangeLog");
                var nameNode = xmlDoc.SelectSingleNode("//DisplayName");
                var modIdNode = xmlDoc.SelectSingleNode("//ModId");
                var thumbnailNode = xmlDoc.SelectSingleNode("//Thumbnail");

                var version = versionNode?.Attributes?["Value"]?.Value ?? "Unknown";
                var changelog = changelogNode?.InnerText[(changelogNode?.InnerText.IndexOf('\n') ?? 0)..].Trim() ?? "Changes not available";
                var name = nameNode?.Attributes?["Value"]?.Value ?? throw new Exception("DisplayName node not found");
                var modId = modIdNode?.Attributes?["Value"]?.Value ??
                            throw new Exception("New mods can't be handled yet");
                var thumbnail = thumbnailNode?.Attributes?["Value"]?.Value;

                // You can set imageBase64 here - either from a file path or pass it as parameter
                var imageBase64 =
                    thumbnail != null
                        ? ConvertTextureToBase64(Path.Combine(projectRoot, thumbnail))
                        : null; // Set this to your base64 image string when you have one

                return
                    ($"{name} was updated to version {version}!\nCheck out at Paradox Mods!\nChangelog:\n{changelog}",
                        $"https://mods.paradoxplaza.com/mods/{modId}/Windows",
                        $"{name} by Klyte45 at Paradox Mods",
                        $"Check out new features from the version {version} of {name} by Klyte45 at Paradox Mods!",
                        imageBase64);
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao processar XML: {ex.Message}");
            }
        }

        private static string PostJson(string url, string payload, string bearerToken = null)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/json";
            if (!string.IsNullOrEmpty(bearerToken))
                request.Headers["Authorization"] = "Bearer " + bearerToken;

            using (var stream = request.GetRequestStream())
            {
                var bytes = Encoding.UTF8.GetBytes(payload);
                stream.Write(bytes, 0, bytes.Length);
            }

            using var response = (HttpWebResponse)request.GetResponse();
            using var reader = new StreamReader(response.GetResponseStream()!);
            return reader.ReadToEnd();
        }

        private static string EscapeJson(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";

            return value
                .Replace("\\", @"\\") // Backslash must be first
                .Replace("\"", "\\\"") // Double quote
                .Replace("\b", "\\b") // Backspace
                .Replace("\f", "\\f") // Form feed
                .Replace("\n", "\\n") // Line feed (newline)
                .Replace("\r", "\\r") // Carriage return
                .Replace("\t", "\\t") // Tab
                .Replace("/", "\\/"); // Forward slash (optional but safer)
        }


        private static string ExtractJsonValue(string json, string key)
        {
            var search = $"\"{key}\":\"";
            var start = json.IndexOf(search, StringComparison.OrdinalIgnoreCase);
            if (start < 0) return "";
            start += search.Length;
            var end = json.IndexOf("\"", start, StringComparison.OrdinalIgnoreCase);
            if (end < 0) return "";
            return json.Substring(start, end - start);
        }

        private static string ConvertTextureToBase64(string texturePath)
        {
            try
            {
                // Check if the path is a file path
                if (File.Exists(texturePath))
                {
                    // Read image file directly
                    var imageBytes = File.ReadAllBytes(texturePath);
                    return Convert.ToBase64String(imageBytes);
                }
                else
                {
                    Console.WriteLine($"Warning: Could not find image file for path: {texturePath}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting texture to base64: {ex.Message}");
                return null;
            }
        }

        private static BlobInfo UploadBlob(string base64Data, string mimeType, string accessJwt)
        {
            var imageBytes = Convert.FromBase64String(base64Data);

            var request = (HttpWebRequest)WebRequest.Create("https://bsky.social/xrpc/com.atproto.repo.uploadBlob");
            request.Method = "POST";
            request.ContentType = mimeType;
            request.Headers["Authorization"] = "Bearer " + accessJwt;
            request.ContentLength = imageBytes.Length;

            using (var stream = request.GetRequestStream())
            {
                stream.Write(imageBytes, 0, imageBytes.Length);
            }

            using var response = (HttpWebResponse)request.GetResponse();
            using var reader = new StreamReader(response.GetResponseStream()!);
            var responseText = reader.ReadToEnd();

            Console.WriteLine($"Blob upload response: {responseText}");

            var cid = ExtractJsonValue(responseText, "cid");
            if (string.IsNullOrEmpty(cid))
            {
                // Try extracting from blob object
                var blobStart = responseText.IndexOf("\"blob\":{", StringComparison.OrdinalIgnoreCase);
                if (blobStart >= 0)
                {
                    var blobSection = responseText.Substring(blobStart);
                    cid = ExtractJsonValue(blobSection, "$link");
                }
            }

            if (!string.IsNullOrEmpty(cid))
            {
                return new BlobInfo
                {
                    cid = cid,
                    mimeType = mimeType,
                    size = imageBytes.Length
                };
            }

            return null;
        }

        private class BlobInfo
        {
            public string cid { get; set; }
            public string mimeType { get; set; }
            public int size { get; set; }
        }
    }
}