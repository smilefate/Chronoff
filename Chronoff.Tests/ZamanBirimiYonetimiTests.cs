namespace Chronoff.Tests;

using System;
using Chronoff;

[TestClass]
public sealed class ZamanBirimiYonetimiTests
{
    [TestMethod]
    public void DakikaArttirma_Saat00_Dakika50_Artis15_Sonuc_Saat01_Dakika05()
    {
        // Kullanıcı senaryosu: Saat 00 dakika 50 ve dakika + 15 arttırılıyorsa
        // sonuç şöyle olmalı: saat 01 dakika 05.
        var sonuc = ZamanBirimiYonetimi.SayacAyarHesapla(
            tag: "Dakika+",
            azaltmaModu: false,
            saatArtisi: 1,
            dakikaArtisi: 15,
            aktifSekmeIndex: 1, // Geri Sayım sekmesi
            mevcutSaat: 0,
            mevcutDakika: 50,
            onIkiSaatModu: false,
            amPm: "ÖÖ",
            gun: 1, ay: 1, yil: 2026
        );

        Assert.AreEqual(1, sonuc.YeniSaat);
        Assert.AreEqual(5, sonuc.YeniDakika);
        Assert.AreEqual(0, sonuc.GunDegisimi);
    }

    [TestMethod]
    public void DakikaArttirma_Saat55_Dakika55_Artis5_Dakika00_SaatArtar()
    {
        var sonuc = ZamanBirimiYonetimi.SayacAyarHesapla(
            tag: "Dakika+",
            azaltmaModu: false,
            saatArtisi: 1,
            dakikaArtisi: 5,
            aktifSekmeIndex: 1,
            mevcutSaat: 0,
            mevcutDakika: 55,
            onIkiSaatModu: false,
            amPm: "ÖÖ",
            gun: 1, ay: 1, yil: 2026
        );

        Assert.AreEqual(1, sonuc.YeniSaat);
        Assert.AreEqual(0, sonuc.YeniDakika);
    }

    [TestMethod]
    public void DakikaArttirma_TarihSaatSekmesinde_GunAtlama()
    {
        var sonuc = ZamanBirimiYonetimi.SayacAyarHesapla(
            tag: "Dakika+",
            azaltmaModu: false,
            saatArtisi: 1,
            dakikaArtisi: 15,
            aktifSekmeIndex: 0, // Tarih & Saat sekmesi
            mevcutSaat: 23,
            mevcutDakika: 50,
            onIkiSaatModu: false,
            amPm: "ÖÖ",
            gun: 1, ay: 1, yil: 2026
        );

        Assert.AreEqual(0, sonuc.YeniSaat);
        Assert.AreEqual(5, sonuc.YeniDakika);
        Assert.AreEqual(1, sonuc.GunDegisimi);
    }

    [TestMethod]
    public void DakikaAzaltma_Saat01_Dakika05_Azalis15_Sonuc_Saat00_Dakika50()
    {
        var sonuc = ZamanBirimiYonetimi.SayacAyarHesapla(
            tag: "Dakika-",
            azaltmaModu: true,
            saatArtisi: 1,
            dakikaArtisi: 15,
            aktifSekmeIndex: 1,
            mevcutSaat: 1,
            mevcutDakika: 5,
            onIkiSaatModu: false,
            amPm: "ÖÖ",
            gun: 1, ay: 1, yil: 2026
        );

        Assert.AreEqual(0, sonuc.YeniSaat);
        Assert.AreEqual(50, sonuc.YeniDakika);
    }
}
