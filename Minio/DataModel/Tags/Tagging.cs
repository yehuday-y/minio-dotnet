/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2020, 2021 MinIO, Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Globalization;
using System.Xml;
using System.Xml.Serialization;

namespace Minio.DataModel.Tags;

[Serializable]
[XmlRoot(ElementName = "Tagging")]
/*
* References for Tagging.
* https://docs.aws.amazon.com/AmazonS3/latest/dev/object-tagging.html
* https://docs.aws.amazon.com/AWSEC2/latest/UserGuide/Using_Tags.html#tag-restrictions
*/
public class Tagging
{
    internal const uint MAX_TAG_COUNT_PER_RESOURCE = 50;
    internal const uint MAX_TAG_COUNT_PER_OBJECT = 10;
    internal const uint MAX_TAG_KEY_LENGTH = 128;
    internal const uint MAX_TAG_VALUE_LENGTH = 256;

    public Tagging()
    {
        TaggingSet = null;
    }

    public Tagging(IDictionary<string, string> tags, bool isObjects)
    {
        if (tags is null)
        {
            TaggingSet = null;
            return;
        }

        var tagging_upper_limit = isObjects ? MAX_TAG_COUNT_PER_OBJECT : MAX_TAG_COUNT_PER_RESOURCE;
        if (tags.Count > tagging_upper_limit)
            throw new ArgumentOutOfRangeException(nameof(tags) + ". Count of tags exceeds maximum limit allowed for " +
                                                  (isObjects ? "objects." : "buckets."));

        foreach (var tag in tags)
        {
            if (!ValidateTagKey(tag.Key)) throw new ArgumentException("Invalid Tagging key " + tag.Key);
            if (!ValidateTagValue(tag.Value)) throw new ArgumentException("Invalid Tagging value " + tag.Value);
        }

        TaggingSet = new TagSet(tags);
    }

    [XmlElement("TagSet")] public TagSet TaggingSet { get; set; }

    [XmlIgnore]
    public IDictionary<string, string> Tags
    {
        get
        {
            if (TaggingSet is null || TaggingSet.Tag.Count == 0) return null;
            var tagMap = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var tag in TaggingSet.Tag) tagMap[tag.Key] = tag.Value;
            return tagMap;
        }
    }

    internal bool ValidateTagKey(string key)
    {
        if (string.IsNullOrEmpty(key) ||
            string.IsNullOrWhiteSpace(key) ||
            key.Length > MAX_TAG_KEY_LENGTH ||
            key.Contains('&'))
            return false;

        return true;
    }

    internal bool ValidateTagValue(string value)
    {
        if (value is null || // Empty or whitespace is allowed
            value.Length > MAX_TAG_VALUE_LENGTH ||
            value.Contains('&'))
            return false;

        return true;
    }

    public string MarshalXML()
    {
        XmlWriter xw = null;

        var str = string.Empty;

        try
        {
            var settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true
            };
            var ns = new XmlSerializerNamespaces();
            ns.Add(string.Empty, string.Empty);

            using var sw = new StringWriter(CultureInfo.InvariantCulture);

            var xs = new XmlSerializer(typeof(Tagging), "");
            using (xw = XmlWriter.Create(sw, settings))
            {
                xs.Serialize(xw, this, ns);
                xw.Flush();
                str = Utils.RemoveNamespaceInXML(sw.ToString());
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
        finally
        {
            xw?.Close();
        }

        return str;
    }

    public static Tagging GetBucketTags(IDictionary<string, string> tags)
    {
        return new Tagging(tags, false);
    }

    public static Tagging GetObjectTags(IDictionary<string, string> tags)
    {
        return new Tagging(tags, true);
    }

    internal string GetTagString()
    {
        if (TaggingSet is null || (TaggingSet.Tag is null && TaggingSet.Tag.Count == 0)) return null;
        var tagStr = "";
        var i = 0;
        foreach (var tag in TaggingSet.Tag)
        {
            var append = i++ < TaggingSet.Tag.Count - 1 ? "&" : "";
            tagStr += tag.Key + "=" + tag.Value + append;
        }

        return tagStr;
    }

    public override string ToString()
    {
        return GetTagString();
    }
}