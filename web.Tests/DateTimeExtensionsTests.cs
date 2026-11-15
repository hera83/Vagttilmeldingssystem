using web.Extensions;

namespace web.Tests;

public class DateTimeExtensionsTests
{
    [Fact]
    public void Vinter_ErUtcPlusEn_OgSommer_ErUtcPlusTo()
    {
        Assert.Equal(new DateTime(2027, 1, 15, 17, 0, 0), new DateTime(2027, 1, 15, 16, 0, 0, DateTimeKind.Utc).ToDanishTime());
        Assert.Equal(new DateTime(2027, 6, 1, 17, 0, 0), new DateTime(2027, 6, 1, 15, 0, 0, DateTimeKind.Utc).ToDanishTime());
    }

    [Fact]
    public void TidspunktIOverspringetTime_GiverNull()
    {
        Assert.Null(new DateTime(2027, 3, 28, 2, 30, 0).FromDanishTimeToUtc());
        Assert.Equal(new DateTime(2027, 3, 28, 1, 0, 0), new DateTime(2027, 3, 28, 3, 0, 0).FromDanishTimeToUtc());
    }

    [Fact]
    public void Tekster_ErDanskeMedKolonOgPunktumEfterUgedag()
    {
        var utc = new DateTime(2027, 4, 17, 13, 5, 0, DateTimeKind.Utc);
        Assert.Equal("lør. 17. apr. 2027 kl. 15:05", utc.ToDanishDateTimeText());
        Assert.Equal("17.04.2027 15:05", utc.ToDanishShortDateTimeText());
        Assert.Equal("15:05", utc.ToDanishTimeText());
    }

    [Fact]
    public void Tidsrum_OverMidnat_MarkeresSomNaesteDag()
    {
        var start = new DateTime(2027, 6, 1, 20, 0, 0, DateTimeKind.Utc);
        Assert.Equal("22:00 – 01:00 (næste dag)", DateTimeExtensions.ToDanishTimeRangeText(start, start.AddHours(3)));
        Assert.Equal("22:00 – 23:30", DateTimeExtensions.ToDanishTimeRangeText(start, start.AddMinutes(90)));
    }

    [Fact]
    public void Periode_EnDagEllerFlere()
    {
        Assert.Equal("fre. 12. mar. 2027", DateTimeExtensions.ToDanishPeriodText(new DateOnly(2027, 3, 12), new DateOnly(2027, 3, 12)));
        Assert.Equal("tor. 27. maj – fre. 28. maj 2027", DateTimeExtensions.ToDanishPeriodText(new DateOnly(2027, 5, 27), new DateOnly(2027, 5, 28)));
    }
}
