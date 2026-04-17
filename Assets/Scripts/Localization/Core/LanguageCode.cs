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
        ru = 10,
        tr = 11, // Turkish
        id = 12, // Indonesian
        hi = 13, // Hindi
        uk = 14, // Ukrainian
        it = 15  // Italian
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
                case SystemLanguage.Turkish:
                    return LanguageCode.tr;
                case SystemLanguage.Indonesian:
                    return LanguageCode.id;
                case SystemLanguage.Hindi:
                    return LanguageCode.hi;
                case SystemLanguage.Ukrainian:
                    return LanguageCode.uk;
                case SystemLanguage.Italian:
                    return LanguageCode.it;
                default:
                    return LanguageCode.en; // Mặc định là Tiếng Anh
            }
        }
    }
}
