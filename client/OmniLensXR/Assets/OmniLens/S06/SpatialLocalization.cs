using System;
using System.Collections.Generic;
using System.Globalization;

namespace OmniLensXR.S06
{
    public enum SpatialLanguage
    {
        English = 1,
        Vietnamese = 2,
    }

    public enum SpatialStringKey
    {
        Home = 1,
        Chat = 2,
        Science3D = 3,
        Document = 4,
        Notes = 5,
        Settings = 6,
        Accessibility = 7,
        Move = 8,
        Resize = 9,
        Pin = 10,
        Restore = 11,
        Close = 12,
        ViewerRequired = 13,
        ResetWorkspace = 14,
    }

    public readonly struct LocalizedText
    {
        public LocalizedText(string value, bool usedEnglishFallback)
        {
            Value = value;
            UsedEnglishFallback = usedEnglishFallback;
        }

        public string Value { get; }

        public bool UsedEnglishFallback { get; }
    }

    public sealed class SpatialStringCatalog
    {
        private readonly IReadOnlyDictionary<SpatialStringKey, string> _english;
        private readonly IReadOnlyDictionary<SpatialStringKey, string> _vietnamese;

        public SpatialStringCatalog()
            : this(DefaultEnglish(), DefaultVietnamese())
        {
        }

        public SpatialStringCatalog(
            IReadOnlyDictionary<SpatialStringKey, string> english,
            IReadOnlyDictionary<SpatialStringKey, string> vietnamese
        )
        {
            _english = CopyAndValidateEnglish(english);
            _vietnamese = Copy(vietnamese);
        }

        public SpatialLanguage CurrentLanguage { get; private set; } =
            SpatialLanguage.English;

        public bool TrySetLanguage(SpatialLanguage language)
        {
            if (!Enum.IsDefined(typeof(SpatialLanguage), language))
            {
                return false;
            }

            CurrentLanguage = language;
            return true;
        }

        public bool TryGet(SpatialStringKey key, out LocalizedText text)
        {
            text = default;
            if (!Enum.IsDefined(typeof(SpatialStringKey), key))
            {
                return false;
            }

            if (
                CurrentLanguage == SpatialLanguage.Vietnamese
                && _vietnamese.TryGetValue(key, out string vietnamese)
                && !string.IsNullOrWhiteSpace(vietnamese)
            )
            {
                text = new LocalizedText(vietnamese, false);
                return true;
            }

            if (!_english.TryGetValue(key, out string english))
            {
                return false;
            }

            text = new LocalizedText(
                english,
                CurrentLanguage == SpatialLanguage.Vietnamese
            );
            return true;
        }

        public string FormatNumber(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            return value.ToString("N2", Culture());
        }

        public string FormatDate(DateTimeOffset value)
        {
            return value.ToString("d", Culture());
        }

        private CultureInfo Culture()
        {
            return CultureInfo.GetCultureInfo(
                CurrentLanguage == SpatialLanguage.Vietnamese ? "vi-VN" : "en-US"
            );
        }

        private static IReadOnlyDictionary<SpatialStringKey, string> CopyAndValidateEnglish(
            IReadOnlyDictionary<SpatialStringKey, string> values
        )
        {
            Dictionary<SpatialStringKey, string> copy = Copy(values);
            foreach (SpatialStringKey key in Enum.GetValues(typeof(SpatialStringKey)))
            {
                if (!copy.TryGetValue(key, out string value) || string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException(
                        $"English localization is missing {key}.",
                        nameof(values)
                    );
                }
            }

            return copy;
        }

        private static Dictionary<SpatialStringKey, string> Copy(
            IReadOnlyDictionary<SpatialStringKey, string> values
        )
        {
            if (values == null)
            {
                return new Dictionary<SpatialStringKey, string>();
            }

            Dictionary<SpatialStringKey, string> copy =
                new Dictionary<SpatialStringKey, string>();
            foreach (KeyValuePair<SpatialStringKey, string> pair in values)
            {
                copy[pair.Key] = pair.Value;
            }

            return copy;
        }

        private static IReadOnlyDictionary<SpatialStringKey, string> DefaultEnglish()
        {
            return new Dictionary<SpatialStringKey, string>
            {
                [SpatialStringKey.Home] = "Home",
                [SpatialStringKey.Chat] = "Chat",
                [SpatialStringKey.Science3D] = "3D Science",
                [SpatialStringKey.Document] = "Document",
                [SpatialStringKey.Notes] = "Notes",
                [SpatialStringKey.Settings] = "Settings",
                [SpatialStringKey.Accessibility] = "Accessibility",
                [SpatialStringKey.Move] = "Move",
                [SpatialStringKey.Resize] = "Resize",
                [SpatialStringKey.Pin] = "Pin",
                [SpatialStringKey.Restore] = "Restore",
                [SpatialStringKey.Close] = "Close",
                [SpatialStringKey.ViewerRequired] = "Viewer required for this test",
                [SpatialStringKey.ResetWorkspace] = "Reset workspace",
            };
        }

        private static IReadOnlyDictionary<SpatialStringKey, string> DefaultVietnamese()
        {
            return new Dictionary<SpatialStringKey, string>
            {
                [SpatialStringKey.Home] = "Trang chủ",
                [SpatialStringKey.Chat] = "Trò chuyện",
                [SpatialStringKey.Science3D] = "Khoa học 3D",
                [SpatialStringKey.Document] = "Tài liệu",
                [SpatialStringKey.Notes] = "Ghi chú",
                [SpatialStringKey.Settings] = "Cài đặt",
                [SpatialStringKey.Accessibility] = "Trợ năng",
                [SpatialStringKey.Move] = "Di chuyển",
                [SpatialStringKey.Resize] = "Đổi kích thước",
                [SpatialStringKey.Pin] = "Ghim",
                [SpatialStringKey.Restore] = "Khôi phục",
                [SpatialStringKey.Close] = "Đóng",
                [SpatialStringKey.ViewerRequired] = "Cần kính để chạy kiểm thử này",
                [SpatialStringKey.ResetWorkspace] = "Đặt lại không gian làm việc",
            };
        }
    }
}
