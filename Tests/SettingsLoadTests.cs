// -------------------------------------------------------------------------------------
// <copyright file="SettingsLoadTests.cs">
//   Copyright (c) 2026
//   Licensed under the MIT License. See LICENSE file in the project root.
// </copyright>
// -------------------------------------------------------------------------------------

namespace StrangeLens.Tests
{
   using System.Drawing;
   using System.IO;

   using NUnit.Framework;

   [TestFixture]
   public class SettingsLoadTests
   {
      [SetUp]
      public void SetUp()
      {
         Lens.ResetForTesting();
         this.tempPath = Path.GetTempFileName();
      }

      [TearDown]
      public void TearDown()
      {
         Lens.ResetForTesting();
         if (File.Exists(this.tempPath))
         {
            File.Delete(this.tempPath);
         }
      }

      private string tempPath;

      private void WriteJson(string json)
      {
         File.WriteAllText(this.tempPath, json);
      }

      [Test]
      public void Load_DarkThemeName_Applies()
      {
         this.WriteJson(@"{""Theme"": ""dark""}");
         Lens.Instance.Load(this.tempPath);
         Assert.That(Lens.Instance.Theme, Is.EqualTo("dark"));
      }

      [Test]
      public void Load_EmptyJsonObject_DoesNotThrow()
      {
         this.WriteJson("{}");
         Assert.That(() => Lens.Instance.Load(this.tempPath), Throws.Nothing);
      }

      [Test]
      public void Load_EmptyThemeName_SetsSystem()
      {
         this.WriteJson(@"{""Theme"": """"}");
         Lens.Instance.Load(this.tempPath);
         Assert.That(Lens.Instance.Theme, Is.EqualTo("system"));
      }

      [Test]
      public void Load_GridSizeAboveMax_ClampsToMax()
      {
         this.WriteJson(@"{""GridSize"": 200}");
         Lens.Instance.Load(this.tempPath);
         Assert.That(Lens.Instance.GridSize, Is.EqualTo(Lens.Defaults.MaxGridSize));
      }

      [Test]
      public void Load_GridSizeBelowMin_ClampsToMin()
      {
         this.WriteJson(@"{""GridSize"": 0}");
         Lens.Instance.Load(this.tempPath);
         Assert.That(Lens.Instance.GridSize, Is.EqualTo(Lens.Defaults.MinGridSize));
      }

      [Test]
      public void Load_GridStyleAboveMax_ClampsToMax()
      {
         this.WriteJson(@"{""GridStyle"": 99}");
         Lens.Instance.Load(this.tempPath);
         Assert.That(Lens.Instance.GridStyle, Is.EqualTo((int)GridStyleOption.DashDotDot));
      }

      [Test]
      public void Load_GridStyleBelowMin_ClampsToMin()
      {
         this.WriteJson(@"{""GridStyle"": -1}");
         Lens.Instance.Load(this.tempPath);
         Assert.That(Lens.Instance.GridStyle, Is.EqualTo((int)GridStyleOption.None));
      }

      [Test]
      public void Load_HeightAboveMax_ClampsToMax()
      {
         this.WriteJson(@"{""Height"": 9999}");
         Lens.Instance.Load(this.tempPath);
         Assert.That(Lens.Instance.Height, Is.EqualTo(Lens.Defaults.MaxHeight));
      }

      [Test]
      public void Load_HeightBelowMin_ClampsToMin()
      {
         this.WriteJson(@"{""Height"": 10}");
         Lens.Instance.Load(this.tempPath);
         Assert.That(Lens.Instance.Height, Is.EqualTo(Lens.Defaults.MinHeight));
      }

      [Test]
      public void Load_InvalidScalingMode_KeepsDefault()
      {
         this.WriteJson(@"{""Scaling"": 99}");
         Lens.Instance.Load(this.tempPath);
         Assert.That(Lens.Instance.Scaling, Is.EqualTo(ScalingModeOption.NearestNeighbor));
      }

      [Test]
      public void Load_MagnificationAboveMax_ClampsToMax()
      {
         this.WriteJson(@"{""Magnification"": 99}");
         Lens.Instance.Load(this.tempPath);
         Assert.That(Lens.Instance.Magnification, Is.EqualTo(Lens.Defaults.MaxMagnification));
      }

      [Test]
      public void Load_MagnificationBelowMin_ClampsToMin()
      {
         this.WriteJson(@"{""Magnification"": 0}");
         Lens.Instance.Load(this.tempPath);
         Assert.That(Lens.Instance.Magnification, Is.EqualTo(Lens.Defaults.MinMagnification));
      }

      [Test]
      public void Load_MalformedJson_DoesNotThrow()
      {
         this.WriteJson("{ this is not valid json }");
         Assert.That(() => Lens.Instance.Load(this.tempPath), Throws.Nothing);
      }

      [Test]
      public void Load_MissingFile_DoesNotThrow()
      {
         File.Delete(this.tempPath);
         Assert.That(() => Lens.Instance.Load(this.tempPath), Throws.Nothing);
      }

      [Test]
      public void Load_PrecisionSpeedNotInOptions_KeepsDefault()
      {
         this.WriteJson(@"{""PrecisionSpeed"": 30}");
         Lens.Instance.Load(this.tempPath);
         Assert.That(Lens.Instance.PrecisionSpeed, Is.EqualTo(45));
      }

      [Test]
      public void Load_SystemThemeName_Applies()
      {
         this.WriteJson(@"{""Theme"": ""system""}");
         Lens.Instance.Load(this.tempPath);
         Assert.That(Lens.Instance.Theme, Is.EqualTo("system"));
      }

      [Test]
      public void Load_UnknownThemeName_FallsBackToOsTheme()
      {
         this.WriteJson(@"{""Theme"": ""nonexistent""}");
         Lens.Instance.Load(this.tempPath);
         Assert.That(Lens.Instance.Theme, Is.EqualTo("dark").Or.EqualTo("light"));
      }

      [Test]
      public void Load_ValidConfig_AppliesAllValues()
      {
         this.WriteJson(
            @"{
            ""Width"": 200, ""Height"": 220, ""Magnification"": 6,
            ""GridSize"": 8, ""GridStyle"": 1, ""GridOpacity"": 40,
            ""Scaling"": 2, ""PrecisionSpeed"": 25, ""InfoShowHex"": false
         }");
         Lens.Instance.Load(this.tempPath);

         Assert.Multiple(() =>
            {
               Assert.That(Lens.Instance.Width, Is.EqualTo(200));
               Assert.That(Lens.Instance.Height, Is.EqualTo(220));
               Assert.That(Lens.Instance.Magnification, Is.EqualTo(6));
               Assert.That(Lens.Instance.GridSize, Is.EqualTo(8));
               Assert.That(Lens.Instance.GridStyle, Is.EqualTo(1));
               Assert.That(Lens.Instance.GridOpacity, Is.EqualTo(40));
               Assert.That(Lens.Instance.Scaling, Is.EqualTo(ScalingModeOption.HighQualityBilinear));
               Assert.That(Lens.Instance.PrecisionSpeed, Is.EqualTo(25));
               Assert.That(Lens.Instance.InfoShowHex, Is.False);
            });
      }

      [Test]
      public void Load_ValidPrecisionSpeed_Applies()
      {
         this.WriteJson(@"{""PrecisionSpeed"": 10}");
         Lens.Instance.Load(this.tempPath);
         Assert.That(Lens.Instance.PrecisionSpeed, Is.EqualTo(10));
      }

      [Test]
      public void Load_WidthAboveMax_ClampsToMax()
      {
         this.WriteJson(@"{""Width"": 9999}");
         Lens.Instance.Load(this.tempPath);
         Assert.That(Lens.Instance.Width, Is.EqualTo(Lens.Defaults.MaxWidth));
      }

      [Test]
      public void Load_WidthBelowMin_ClampsToMin()
      {
         this.WriteJson(@"{""Width"": 10}");
         Lens.Instance.Load(this.tempPath);
         Assert.That(Lens.Instance.Width, Is.EqualTo(Lens.Defaults.MinWidth));
      }

      [Test]
      public void Load_WidthNotOnIncrement_SnapsDown()
      {
         this.WriteJson(@"{""Width"": 155}");
         Lens.Instance.Load(this.tempPath);
         Assert.That(Lens.Instance.Width, Is.EqualTo(140));
      }
   }
}
