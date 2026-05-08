using ImeWlConverter.Core.Adapters;
using Xunit;
using OldCodeType = Studyzy.IMEWLConverter.Entities.CodeType;
using NewCodeType = ImeWlConverter.Abstractions.Enums.CodeType;

namespace Studyzy.IMEWLConverter.Tests.Adapters;

public class LegacyImporterAdapterTest
{
    [Theory]
    [InlineData(OldCodeType.Pinyin, NewCodeType.Pinyin)]
    [InlineData(OldCodeType.Wubi, NewCodeType.Wubi86)]
    [InlineData(OldCodeType.Wubi98, NewCodeType.Wubi98)]
    [InlineData(OldCodeType.WubiNewAge, NewCodeType.WubiNewAge)]
    [InlineData(OldCodeType.Zhengma, NewCodeType.Zhengma)]
    [InlineData(OldCodeType.Cangjie, NewCodeType.Cangjie5)]
    [InlineData(OldCodeType.TerraPinyin, NewCodeType.TerraPinyin)]
    [InlineData(OldCodeType.Zhuyin, NewCodeType.Zhuyin)]
    [InlineData(OldCodeType.English, NewCodeType.English)]
    [InlineData(OldCodeType.UserDefine, NewCodeType.UserDefine)]
    [InlineData(OldCodeType.NoCode, NewCodeType.NoCode)]
    [InlineData(OldCodeType.QingsongErbi, NewCodeType.QingsongErbi)]
    [InlineData(OldCodeType.ChaoqiangErbi, NewCodeType.ChaoqiangErbi)]
    [InlineData(OldCodeType.XiandaiErbi, NewCodeType.XiandaiErbi)]
    [InlineData(OldCodeType.Chaoyin, NewCodeType.Chaoyin)]
    public void MapCodeType_MapsCorrectly(OldCodeType old, NewCodeType expected)
    {
        var result = LegacyImporterAdapter.MapCodeType(old);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(OldCodeType.Unknown, NewCodeType.Unknown)]
    [InlineData(OldCodeType.Yong, NewCodeType.Yong)]
    [InlineData(OldCodeType.InnerCode, NewCodeType.InnerCode)]
    [InlineData(OldCodeType.UserDefinePhrase, NewCodeType.UserDefinePhrase)]
    public void MapCodeType_UnmappedValues_DefaultToPinyin(OldCodeType old, NewCodeType expected)
    {
        var result = LegacyImporterAdapter.MapCodeType(old);
        Assert.Equal(expected, result);
    }
}
