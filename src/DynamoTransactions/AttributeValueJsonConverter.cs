using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;
using Newtonsoft.Json;

namespace DynamoTransactions
{
    public class AttributeValueJsonConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var attributeValue = (AttributeValue)value;
            writer.WriteStartObject();

            if (attributeValue.IsBOOLSet)
            {
                writer.WritePropertyName("BOOL");
                writer.WriteValue(attributeValue.BOOL);
            }

            if (attributeValue.IsLSet)
            {
                writer.WritePropertyName("L");
                serializer.Serialize(writer, attributeValue.L);
            }

            if (attributeValue.IsMSet)
            {
                writer.WritePropertyName("M");
                serializer.Serialize(writer, attributeValue.M);
            }

            if (attributeValue.NULL)
            {
                writer.WritePropertyName("NULL");
                writer.WriteValue(true);
            }

            if (attributeValue.B != null && attributeValue.B.Length > 0)
            {
                writer.WritePropertyName("B");
                serializer.Serialize(writer, attributeValue.B);
            }

            if (attributeValue.BS != null && attributeValue.BS.Count > 0)
            {
                writer.WritePropertyName("BS");
                serializer.Serialize(writer, attributeValue.BS);
            }

            if (!string.IsNullOrEmpty(attributeValue.N))
            {
                writer.WritePropertyName("N");
                writer.WriteValue(attributeValue.N);
            }

            if (attributeValue.NS != null && attributeValue.NS.Count > 0)
            {
                writer.WritePropertyName("NS");
                serializer.Serialize(writer, attributeValue.NS);
            }

            if (!string.IsNullOrEmpty(attributeValue.S))
            {
                writer.WritePropertyName("S");
                writer.WriteValue(attributeValue.S);
            }

            if (attributeValue.SS != null && attributeValue.SS.Count > 0)
            {
                writer.WritePropertyName("SS");
                serializer.Serialize(writer, attributeValue.SS);
            }

            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            Debug.Assert(reader.TokenType == JsonToken.StartObject);
            reader.Read(); // read property name token

            var attributeValue = new AttributeValue();
            while (reader.TokenType == JsonToken.PropertyName)
            {
                string propertyName = (string) reader.Value;

                MemoryStream b = null;
                List<MemoryStream> bs = new List<MemoryStream>();

                switch (propertyName)
                {
                    case "B":
                        reader.Read();
                        Debug.Assert(reader.TokenType == JsonToken.String);
                        attributeValue.B = serializer.Deserialize<MemoryStream>(reader);
                        break;
                    case "BOOL":
                        var @bool = reader.ReadAsBoolean();
                        if (@bool != null)
                        {
                            attributeValue.BOOL = @bool.Value;
                        }
                        break;
                    case "BS":
                        reader.Read();
                        Debug.Assert(reader.TokenType == JsonToken.StartArray);
                        attributeValue.BS = serializer.Deserialize<List<MemoryStream>>(reader);
                        Debug.Assert(reader.TokenType == JsonToken.EndArray);
                        break;
                    case "L":
                        reader.Read();
                        Debug.Assert(reader.TokenType == JsonToken.StartArray);
                        var l = serializer.Deserialize<List<AttributeValue>>(reader);
                        Debug.Assert(reader.TokenType == JsonToken.EndArray);
                        if (l != null)
                        {
                            attributeValue.L = l;
                        }
                        break;
                    case "M":
                        reader.Read();
                        Debug.Assert(reader.TokenType == JsonToken.StartObject);
                        var m = serializer.Deserialize<Dictionary<string, AttributeValue>>(reader);
                        Debug.Assert(reader.TokenType == JsonToken.EndObject);
                        if (m != null)
                        {
                            attributeValue.M = m;
                        }
                        break;
                    case "N":
                        attributeValue.N = reader.ReadAsString();
                        break;
                    case "NULL":
                        var @null = reader.ReadAsBoolean();
                        if (@null != null)
                        {
                            attributeValue.NULL = @null.Value;
                        }
                        break;
                    case "NS":
                        reader.Read();
                        Debug.Assert(reader.TokenType == JsonToken.StartArray);
                        attributeValue.NS = serializer.Deserialize<List<string>>(reader);
                        Debug.Assert(reader.TokenType == JsonToken.EndArray);
                        break;
                    case "S":
                        attributeValue.S = reader.ReadAsString();
                        break;
                    case "SS":
                        reader.Read();
                        Debug.Assert(reader.TokenType == JsonToken.StartArray);
                        attributeValue.SS = serializer.Deserialize<List<string>>(reader);
                        Debug.Assert(reader.TokenType == JsonToken.EndArray);
                        break;
                    default:
                        serializer.Deserialize(reader);
                        break;
                }

                reader.Read(); // read next token
            }
            Debug.Assert(reader.TokenType == JsonToken.EndObject);
            //reader.Read(); // read end object token
            return attributeValue;
        }

        public override bool CanConvert(Type objectType) => objectType == typeof(AttributeValue);
    }
}
