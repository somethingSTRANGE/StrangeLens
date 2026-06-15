using System.Drawing;
using System.IO;
using NUnit.Framework;

namespace Lens.Tests
{
   [TestFixture]
   public class SettingsLoadTests
   {
      private string _tempPath;

      [SetUp]
      public void SetUp()
      {
         Lens.ResetForTesting();
         _tempPath = Path.GetTempFileName();
      }

      [TearDown]
      public void TearDown()
      {
         Lens.ResetForTesting();
         if (File.Exists(_tempPath)) File.Delete(_tempPath);
      }

      private void WriteJson(string json) => File.WriteAllText(_tempPath, json);

      // ── Valid config ───────────────────────────────────────────────────────────

      [Test]
      public void Load_ValidConfig_AppliesAllValues()
      {
         WriteJson(@"{
            ""Width"": 200, ""Height"": 220, ""Magnification"": 6,
            ""GridSize"": 8, ""GridStyle"": 1, ""GridColor"": ""#FF0000"",
            ""Scaling"": 2, ""PrecisionSpeed"": 25, ""InfoShowHex"": false
         }");
         Lens.Instance.Load(_tempPath);

         Assert.Multiple(() =>
         {
            Assert.That(Lens.Instance.Width,              Is.EqualTo(200));
            Assert.That(Lens.Instance.Height,             Is.EqualTo(220));
            Assert.That(Lens.Instance.Magnification,      Is.EqualTo(6));
            Assert.That(Lens.Instance.GridSize,           Is.EqualTo(8));
            Assert.That(Lens.Instance.GridStyle,          Is.EqualTo(1));
            Assert.That(Lens.Instance.GridColor.ToArgb(), Is.EqualTo(Color.FromArgb(255, 0, 0).ToArgb()));
            Assert.That(Lens.Instance.Scaling,            Is.EqualTo(ScalingMode.HighQualityBilinear));
            Assert.That(Lens.Instance.PrecisionSpeed,     Is.EqualTo(25));
            Assert.That(Lens.Instance.InfoShowHex,        Is.False);
         });
      }

      // ── Width / Height ─────────────────────────────────────────────────────────

      [Test]
      public void Load_WidthAboveMax_ClampsToMax()
      {
         WriteJson(@"{""Width"": 9999}");
         Lens.Instance.Load(_tempPath);
         Assert.That(Lens.Instance.Width, Is.EqualTo(Lens.Defaults.MaxWidth));
      }

      [Test]
      public void Load_WidthBelowMin_ClampsToMin()
      {
         WriteJson(@"{""Width"": 10}");
         Lens.Instance.Load(_tempPath);
         Assert.That(Lens.Instance.Width, Is.EqualTo(Lens.Defaults.MinWidth));
      }

      [Test]
      public void Load_WidthNotOnIncrement_SnapsDown()
      {
         WriteJson(@"{""Width"": 155}");
         Lens.Instance.Load(_tempPath);
         Assert.That(Lens.Instance.Width, Is.EqualTo(140));
      }

      [Test]
      public void Load_HeightAboveMax_ClampsToMax()
      {
         WriteJson(@"{""Height"": 9999}");
         Lens.Instance.Load(_tempPath);
         Assert.That(Lens.Instance.Height, Is.EqualTo(Lens.Defaults.MaxHeight));
      }

      [Test]
      public void Load_HeightBelowMin_ClampsToMin()
      {
         WriteJson(@"{""Height"": 10}");
         Lens.Instance.Load(_tempPath);
         Assert.That(Lens.Instance.Height, Is.EqualTo(Lens.Defaults.MinHeight));
      }

      // ── Magnification / GridSize / GridStyle ───────────────────────────────────

      [Test]
      public void Load_MagnificationAboveMax_ClampsToMax()
      {
         WriteJson(@"{""Magnification"": 99}");
         Lens.Instance.Load(_tempPath);
         Assert.That(Lens.Instance.Magnification, Is.EqualTo(Lens.Defaults.MaxMagnification));
      }

      [Test]
      public void Load_MagnificationBelowMin_ClampsToMin()
      {
         WriteJson(@"{""Magnification"": 0}");
         Lens.Instance.Load(_tempPath);
         Assert.That(Lens.Instance.Magnification, Is.EqualTo(Lens.Defaults.MinMagnification));
      }

      [Test]
      public void Load_GridSizeAboveMax_ClampsToMax()
      {
         WriteJson(@"{""GridSize"": 200}");
         Lens.Instance.Load(_tempPath);
         Assert.That(Lens.Instance.GridSize, Is.EqualTo(Lens.Defaults.MaxGridSize));
      }

      [Test]
      public void Load_GridSizeBelowMin_ClampsToMin()
      {
         WriteJson(@"{""GridSize"": 0}");
         Lens.Instance.Load(_tempPath);
         Assert.That(Lens.Instance.GridSize, Is.EqualTo(Lens.Defaults.MinGridSize));
      }

      [Test]
      public void Load_GridStyleAboveMax_ClampsToMax()
      {
         WriteJson(@"{""GridStyle"": 99}");
         Lens.Instance.Load(_tempPath);
         Assert.That(Lens.Instance.GridStyle, Is.EqualTo((int)GridStyleOptions.DashDotDot));
      }

      [Test]
      public void Load_GridStyleBelowMin_ClampsToMin()
      {
         WriteJson(@"{""GridStyle"": -1}");
         Lens.Instance.Load(_tempPath);
         Assert.That(Lens.Instance.GridStyle, Is.EqualTo((int)GridStyleOptions.None));
      }

      // ── GridColor ──────────────────────────────────────────────────────────────

      [Test]
      public void Load_InvalidGridColor_FallsBackToBlack()
      {
         WriteJson(@"{""GridColor"": ""not-a-color""}");
         Lens.Instance.Load(_tempPath);
         Assert.That(Lens.Instance.GridColor.ToArgb(), Is.EqualTo(Color.Black.ToArgb()));
      }

      // ── ScalingMode ────────────────────────────────────────────────────────────

      [Test]
      public void Load_InvalidScalingMode_KeepsDefault()
      {
         WriteJson(@"{""Scaling"": 99}");
         Lens.Instance.Load(_tempPath);
         Assert.That(Lens.Instance.Scaling, Is.EqualTo(ScalingMode.NearestNeighbor));
      }

      // ── PrecisionSpeed ─────────────────────────────────────────────────────────

      [Test]
      public void Load_PrecisionSpeedNotInOptions_KeepsDefault()
      {
         WriteJson(@"{""PrecisionSpeed"": 30}");
         Lens.Instance.Load(_tempPath);
         Assert.That(Lens.Instance.PrecisionSpeed, Is.EqualTo(45));
      }

      [Test]
      public void Load_ValidPrecisionSpeed_Applies()
      {
         WriteJson(@"{""PrecisionSpeed"": 10}");
         Lens.Instance.Load(_tempPath);
         Assert.That(Lens.Instance.PrecisionSpeed, Is.EqualTo(10));
      }

      // ── Theme ──────────────────────────────────────────────────────────────────

      [Test]
      public void Load_EmptyThemeName_SetsSystem()
      {
         WriteJson(@"{""Theme"": """"}");
         Lens.Instance.Load(_tempPath);
         Assert.That(Lens.Instance.Theme, Is.EqualTo("system"));
      }

      [Test]
      public void Load_SystemThemeName_Applies()
      {
         WriteJson(@"{""Theme"": ""system""}");
         Lens.Instance.Load(_tempPath);
         Assert.That(Lens.Instance.Theme, Is.EqualTo("system"));
      }

      [Test]
      public void Load_DarkThemeName_Applies()
      {
         WriteJson(@"{""Theme"": ""dark""}");
         Lens.Instance.Load(_tempPath);
         Assert.That(Lens.Instance.Theme, Is.EqualTo("dark"));
      }

      [Test]
      public void Load_UnknownThemeName_FallsBackToOsTheme()
      {
         WriteJson(@"{""Theme"": ""nonexistent""}");
         Lens.Instance.Load(_tempPath);
         Assert.That(Lens.Instance.Theme, Is.EqualTo("dark").Or.EqualTo("light"));
      }

      // ── Edge cases ─────────────────────────────────────────────────────────────

      [Test]
      public void Load_MissingFile_DoesNotThrow()
      {
         File.Delete(_tempPath);
         Assert.That(() => Lens.Instance.Load(_tempPath), Throws.Nothing);
      }

      [Test]
      public void Load_MalformedJson_DoesNotThrow()
      {
         WriteJson("{ this is not valid json }");
         Assert.That(() => Lens.Instance.Load(_tempPath), Throws.Nothing);
      }

      [Test]
      public void Load_EmptyJsonObject_DoesNotThrow()
      {
         WriteJson("{}");
         Assert.That(() => Lens.Instance.Load(_tempPath), Throws.Nothing);
      }
   }
}
