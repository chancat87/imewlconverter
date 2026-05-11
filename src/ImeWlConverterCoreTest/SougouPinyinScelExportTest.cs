// SougouScel exporter has not been migrated to the new architecture yet.
// These tests are skipped until the exporter is available.

using Xunit;

namespace Studyzy.IMEWLConverter.Test;

public class SougouPinyinScelExportTest
{
    [Fact(Skip = "SougouScel exporter not yet migrated to new architecture")]
    public void TestExportBasicScel() { }

    [Fact(Skip = "SougouScel exporter not yet migrated to new architecture")]
    public void TestRoundTrip() { }
}
