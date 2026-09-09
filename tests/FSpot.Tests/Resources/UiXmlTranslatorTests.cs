// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Resources.Lang;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Tests.Resources
{
	[TestClass]
	public class UiXmlTranslatorTests
	{
		[TestMethod]
		public void Translate_ReplacesTranslatablePropertyText ()
		{
			const string xml = """
				<interface>
				  <object class="GtkAction" id="file1">
				    <property name="label" translatable="yes">_Photo</property>
				    <property name="name">file1</property>
				  </object>
				</interface>
				""";

			var translated = UiXmlTranslator.Translate (xml, s => s == "_Photo" ? "_Foto" : s);

			Assert.Contains ("_Foto", translated);
			Assert.DoesNotContain (">_Photo<", translated);
			Assert.Contains ("file1", translated);
		}

		[TestMethod]
		public void Translate_LeavesNonTranslatablePropertiesAlone ()
		{
			const string xml = """
				<interface>
				  <property name="label" translatable="no">_Photo</property>
				</interface>
				""";

			var translated = UiXmlTranslator.Translate (xml, _ => "nope");

			Assert.Contains ("_Photo", translated);
			Assert.DoesNotContain ("nope", translated);
		}

		[TestMethod]
		public void Translate_ReplacesTranslatableColText ()
		{
			const string xml = """
				<interface>
				  <object class="GtkListStore" id="liststore3">
				    <data>
				      <row>
				        <col id="0" translatable="yes">at</col>
				      </row>
				    </data>
				  </object>
				</interface>
				""";

			var translated = UiXmlTranslator.Translate (xml, s => s == "at" ? "um" : s);

			Assert.Contains (">um<", translated);
			Assert.DoesNotContain (">at<", translated);
		}

		[TestMethod]
		public void Translate_PreservesXmlEntitiesInMarkupLabels ()
		{
			const string xml = """
				<interface>
				  <property name="label" translatable="yes">&lt;b&gt;Photos&lt;/b&gt;</property>
				</interface>
				""";

			var translated = UiXmlTranslator.Translate (xml, s => s == "<b>Photos</b>" ? "<b>Fotos</b>" : s);

			Assert.Contains ("&lt;b&gt;Fotos&lt;/b&gt;", translated);
		}
	}
}
