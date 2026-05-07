using System.Collections.Generic;
using Studyzy.IMEWLConverter.IME;

namespace Studyzy.IMEWLConverter;

/// <summary>
/// Registers all known IME formats without reflection.
/// </summary>
internal static class FormatRegistrar
{
    public static (Dictionary<string, IWordLibraryImport> imports, Dictionary<string, IWordLibraryExport> exports)
        RegisterAll()
    {
        var imports = new Dictionary<string, IWordLibraryImport>();
        var exports = new Dictionary<string, IWordLibraryExport>();

        // Import + Export
        Register(imports, exports, ConstantString.SOUGOU_PINYIN_C, new SougouPinyin());
        Register(imports, exports, ConstantString.SOUGOU_XIBAO_SCEL_C, new SougouPinyinScel());
        Register(imports, exports, ConstantString.BAIDU_PINYIN_C, new BaiduPinyin());
        Register(imports, exports, ConstantString.BAIDU_SHOUJI_C, new BaiduShouji());
        Register(imports, exports, ConstantString.BAIDU_SHOUJI_ENG_C, new BaiduShoujiEng());
        Register(imports, exports, ConstantString.GOOGLE_PINYIN_C, new GooglePinyin());
        Register(imports, exports, ConstantString.GBOARD_C, new Gboard());
        Register(imports, exports, ConstantString.RIME_C, new Rime());
        Register(imports, exports, ConstantString.FIT_C, new FitInput());
        Register(imports, exports, ConstantString.MS_PINYIN_C, new MsPinyin());
        Register(imports, exports, ConstantString.WIN10_MS_PINYIN_C, new Win10MsPinyin());
        Register(imports, exports, ConstantString.WIN10_MS_WUBI_C, new Win10MsWubi());
        Register(imports, exports, ConstantString.WIN10_MS_PINYIN_SELF_STUDY_C, new Win10MsPinyinSelfStudy());
        Register(imports, exports, ConstantString.SINA_PINYIN_C, new SinaPinyin());
        Register(imports, exports, ConstantString.SHOUXIN_PINYIN_C, new ShouxinPinyin());
        Register(imports, exports, ConstantString.CHINESE_PYIM_C, new ChinesePyim());
        Register(imports, exports, ConstantString.CANGJIE_PLATFORM_C, new CangjiePlatform());
        Register(imports, exports, ConstantString.CHAO_YIN_C, new Chaoyin());
        Register(imports, exports, ConstantString.YAHOO_KEYKEY_C, new YahooKeyKey());
        Register(imports, exports, ConstantString.QQ_SHOUJI_C, new QQShouji());
        Register(imports, exports, ConstantString.WORD_ONLY_C, new NoPinyinWordOnly());
        Register(imports, exports, ConstantString.JIDIAN_C, new Jidian());
        Register(imports, exports, ConstantString.WUBI86_C, new Wubi86());
        Register(imports, exports, ConstantString.WUBI98_C, new Wubi98());
        Register(imports, exports, ConstantString.WUBI_NEWAGE_C, new WubiNewAge());
        Register(imports, exports, ConstantString.XIAOYA_WUBI_C, new XiaoyaWubi());
        Register(imports, exports, ConstantString.IFLY_IME_C, new iFlyIME());
        Register(imports, exports, ConstantString.PINYIN_JIAJIA_C, new PinyinJiaJia());
        Register(imports, exports, ConstantString.ZIGUANG_PINYIN_C, new ZiGuangPinyin());

        // Import only
        Register(imports, exports, ConstantString.SOUGOU_PINYIN_BIN_C, new SougouPinyinBinFromPython());
        Register(imports, exports, ConstantString.BAIDU_PINYIN_BACKUP_C, new BaiduPinyinBackup());
        Register(imports, exports, ConstantString.BAIDU_BDICT_C, new BaiduPinyinBdict());
        Register(imports, exports, ConstantString.BAIDU_BCD_C, new BaiduShoujiBcd());
        Register(imports, exports, ConstantString.QQ_PINYIN_QPYD_C, new QQPinyinQpyd());
        Register(imports, exports, ConstantString.QQ_PINYIN_QCEL_C, new QQPinyinQcel());
        Register(imports, exports, ConstantString.LINGOES_LD2_C, new LingoesLd2());
        Register(imports, exports, ConstantString.RIME_USERDB_C, new RimeUserDb());
        Register(imports, exports, ConstantString.ZIGUANG_PINYIN_UWL_C, new ZiGuangPinyinUwl());
        Register(imports, exports, ConstantString.JIDIAN_MBDICT_C, new Jidian_MBDict());
        Register(imports, exports, ConstantString.EMOJI_C, new Emoji());

        // Export only
        Register(imports, exports, ConstantString.QQ_PINYIN_C, new QQPinyin());
        Register(imports, exports, ConstantString.QQ_PINYIN_ENG_C, new QQPinyinEng());
        Register(imports, exports, ConstantString.QQ_WUBI_C, new QQWubi());
        Register(imports, exports, ConstantString.BING_PINYIN_C, new BingPinyin());
        Register(imports, exports, ConstantString.LIBPINYIN_C, new Libpinyin());
        Register(imports, exports, ConstantString.MAC_PLIST_C, new MacPlist());
        Register(imports, exports, ConstantString.XIAOXIAO_C, new Xiaoxiao());
        Register(imports, exports, ConstantString.XIAOXIAO_ERBI_C, new XiaoxiaoErbi());
        Register(imports, exports, ConstantString.JIDIAN_ZHENGMA_C, new JidianZhengma());
        Register(imports, exports, ConstantString.SELF_DEFINING_C, new SelfDefining());
        Register(imports, exports, ConstantString.USER_PHRASE_C, new UserDefinePhrase());
        Register(imports, exports, ConstantString.LIBIME_TEXT_C, new LibIMEText());

        return (imports, exports);
    }

    private static void Register<T>(
        Dictionary<string, IWordLibraryImport> imports,
        Dictionary<string, IWordLibraryExport> exports,
        string code,
        T instance)
    {
        if (instance is IWordLibraryImport importer)
            imports[code] = importer;
        if (instance is IWordLibraryExport exporter)
            exports[code] = exporter;
    }
}
