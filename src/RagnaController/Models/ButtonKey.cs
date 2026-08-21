using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RagnaController.Models
{
    /// <summary>
    /// ARCH-003: Strongly-typed key for button mappings.
    /// Replaces string-based keys with VirtualKey + optional modifier for compile-time safety.
    /// JSON serialization uses string format "VK" or "Mod+VK" for backward compatibility.
    /// </summary>
    [JsonConverter(typeof(ButtonKeyJsonConverter))]
    public readonly struct ButtonKey : IEquatable<ButtonKey>, IComparable<ButtonKey>
    {
        public readonly VirtualKey Key;
        public readonly VirtualKey Modifier;

        public ButtonKey(VirtualKey key, VirtualKey modifier = VirtualKey.None)
        {
            Key = key;
            Modifier = modifier;
        }

        public static ButtonKey FromVirtualKey(VirtualKey key) => new(key, VirtualKey.None);
        public static ButtonKey FromVirtualKeys(VirtualKey modifier, VirtualKey key) => new(key, modifier);

        public bool HasModifier => Modifier != VirtualKey.None;
        public bool IsNone => Key == VirtualKey.None;

        /// <summary>Parse from string format: "VK" or "Mod+VK" (e.g., "A" or "ControlLeft+A")</summary>
        public static ButtonKey Parse(string s)
        {
            if (string.IsNullOrEmpty(s))
                return default;

            var parts = s.Split('+', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
            {
                if (Enum.TryParse<VirtualKey>(parts[0], true, out var key))
                    return new ButtonKey(key, VirtualKey.None);
                return default;
            }
            else if (parts.Length == 2)
            {
                if (Enum.TryParse<VirtualKey>(parts[0], true, out var mod) &&
                    Enum.TryParse<VirtualKey>(parts[1], true, out var key))
                    return new ButtonKey(key, mod);
                return default;
            }
            return default;
        }

        /// <summary>Convert to string format for JSON serialization and display</summary>
        public override string ToString()
        {
            if (Key == VirtualKey.None)
                return "";
            
            if (Modifier != VirtualKey.None)
                return $"{Modifier}+{Key}";
            
            return Key.ToString();
        }

        public bool Equals(ButtonKey other) => Key == other.Key && Modifier == other.Modifier;
        public override bool Equals(object? obj) => obj is ButtonKey other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Key, Modifier);
        
        public int CompareTo(ButtonKey other)
        {
            var keyCompare = Key.CompareTo(other.Key);
            if (keyCompare != 0) return keyCompare;
            return Modifier.CompareTo(other.Modifier);
        }

        public static bool operator ==(ButtonKey left, ButtonKey right) => left.Equals(right);
        public static bool operator !=(ButtonKey left, ButtonKey right) => !left.Equals(right);
        public static bool operator <(ButtonKey left, ButtonKey right) => left.CompareTo(right) < 0;
        public static bool operator >(ButtonKey left, ButtonKey right) => left.CompareTo(right) > 0;
        public static bool operator <=(ButtonKey left, ButtonKey right) => left.CompareTo(right) <= 0;
        public static bool operator >=(ButtonKey left, ButtonKey right) => left.CompareTo(right) >= 0;

        public static implicit operator ButtonKey(VirtualKey key) => new(key);
        public static implicit operator ButtonKey((VirtualKey mod, VirtualKey key) tuple) => new(tuple.key, tuple.mod);
    }

    /// <summary>JSON converter for ButtonKey - serializes as "VK" or "Mod+VK" string</summary>
    public class ButtonKeyJsonConverter : JsonConverter<ButtonKey>
    {
        public override ButtonKey Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var s = reader.GetString();
            return string.IsNullOrEmpty(s) ? default : ButtonKey.Parse(s);
        }

        public override void Write(Utf8JsonWriter writer, ButtonKey value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }

    /// <summary>Helper for creating ButtonKey tuples with cleaner syntax</summary>
    public static class ButtonKeys
    {
        // Face buttons
        public static ButtonKey A => VirtualKey.A;
        public static ButtonKey B => VirtualKey.B;
        public static ButtonKey X => VirtualKey.X;
        public static ButtonKey Y => VirtualKey.Y;
        
        // D-Pad
        public static ButtonKey Up => VirtualKey.ArrowUp;
        public static ButtonKey Down => VirtualKey.ArrowDown;
        public static ButtonKey Left => VirtualKey.ArrowLeft;
        public static ButtonKey Right => VirtualKey.ArrowRight;
        
        // Triggers/Shoulders
        public static ButtonKey L1 => VirtualKey.ControlLeft;
        public static ButtonKey R1 => VirtualKey.AltLeft;
        public static ButtonKey L2 => VirtualKey.ShiftLeft;
        public static ButtonKey R2 => VirtualKey.ShiftRight;
        
        // System
        public static ButtonKey Start => VirtualKey.Enter;
        public static ButtonKey Back => VirtualKey.Escape;
        public static ButtonKey Home => VirtualKey.None; // Special handling
        
        // Modifiers
        public static ButtonKey Ctrl(VirtualKey key) => new(key, VirtualKey.ControlLeft);
        public static ButtonKey Alt(VirtualKey key) => new(key, VirtualKey.AltLeft);
        public static ButtonKey Shift(VirtualKey key) => new(key, VirtualKey.ShiftLeft);
    }
}