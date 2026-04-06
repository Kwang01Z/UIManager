using System;
using UnityEngine;

namespace Runtime.Localization
{
    public enum LanguageCode
    {
        Auto = 0,
        en = 1,
        vi = 2,
        ja = 3,
        ko = 4,
        zh = 5,
        fr = 6,
        de = 7,
        es = 8,
        pt = 9,
        ru = 10
    }

    public static class LanguageUtility
    {
        public static LanguageCode GetSystemLanguage()
        {
            switch (Application.systemLanguage)
            {
                case SystemLanguage.Vietnamese:
                    return LanguageCode.vi;
                case SystemLanguage.Japanese:
                    return LanguageCode.ja;
                case SystemLanguage.Korean:
                    return LanguageCode.ko;
                case SystemLanguage.Chinese:
                case SystemLanguage.ChineseSimplified:
                case SystemLanguage.ChineseTraditional:
                    return LanguageCode.zh;
                case SystemLanguage.French:
                    return LanguageCode.fr;
                case SystemLanguage.German:
                    return LanguageCode.de;
                case SystemLanguage.Spanish:
                    return LanguageCode.es;
                case SystemLanguage.Portuguese:
                    return LanguageCode.pt;
                case SystemLanguage.Russian:
                    return LanguageCode.ru;
                default:
                    return LanguageCode.en; // Mặc định là Tiếng Anh
            }
        }
    }
}
