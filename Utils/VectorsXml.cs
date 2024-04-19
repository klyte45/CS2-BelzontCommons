
using System.Xml.Serialization;
using Unity.Mathematics;
using UnityEngine;

namespace Belzont.Utils
{
    [XmlRoot("Vector2")]

    public class Vector2Xml
    {
        [XmlAttribute("x")]
        public float x { get; set; }
        [XmlAttribute("y")]
        public float y { get; set; }


        public static implicit operator Vector2(Vector2Xml v) => new Vector2(v?.x ?? 0, v?.y ?? 0);
        public static explicit operator Vector2Xml(Vector2 v) => new Vector2Xml { x = v.x, y = v.y };
        public static implicit operator float2(Vector2Xml v) => new float2(v?.x ?? 0, v?.y ?? 0);
        public static explicit operator Vector2Xml(float2 v) => new Vector2Xml { x = v.x, y = v.y };

        public override string ToString() => $"Vector2Xml({x},{y})";
    }

    [XmlRoot("Vector3")]

    public class Vector3Xml : Vector2Xml
    {
        [XmlAttribute("z")]
        public float z { get; set; }


        public static implicit operator Vector3(Vector3Xml v) => new Vector3(v?.x ?? 0, v?.y ?? 0, v?.z ?? 0);
        public static explicit operator Vector3Xml(Vector3 v) => new Vector3Xml { x = v.x, y = v.y, z = v.z };
        public static implicit operator float3(Vector3Xml v) => new float3(v?.x ?? 0, v?.y ?? 0, v?.z ?? 0);
        public static explicit operator Vector3Xml(float3 v) => new Vector3Xml { x = v.x, y = v.y, z = v.z };
        public override string ToString() => $"Vector3Xml({x},{y},{z})";
    }

    [XmlRoot("Vector4")]

    public class Vector4Xml : Vector3Xml
    {
        [XmlAttribute("w")]
        public float w { get; set; }


        public static implicit operator Vector4(Vector4Xml v) => new Vector4(v?.x ?? 0, v?.y ?? 0, v?.z ?? 0, v?.w ?? 0);
        public static explicit operator Vector4Xml(Vector4 v) => new Vector4Xml { x = v.x, y = v.y, z = v.z, w = v.w };
        public static implicit operator float4(Vector4Xml v) => new float4(v?.x ?? 0, v?.y ?? 0, v?.z ?? 0, v?.w ?? 0);
        public static explicit operator Vector4Xml(float4 v) => new Vector4Xml { x = v.x, y = v.y, z = v.z, w = v.w };
        public override string ToString() => $"Vector4Xml({x},{y},{z},{w})";
    }



}
