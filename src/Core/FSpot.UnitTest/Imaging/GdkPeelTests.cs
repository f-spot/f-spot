//
// GdkPeelTests.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml.Linq;

using FSpot.Core;
using FSpot.Imaging;
using FSpot.Thumbnail;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.UnitTest
{
	[TestClass]
	public class GdkPeelTests
	{
		[TestMethod]
		public void DomainImageApisDoNotExposePixbuf ()
		{
			var leaks = new[] {
				typeof (IImageFile),
				typeof (IThumbnailService),
				typeof (Photo),
				typeof (Tag),
				typeof (RequestItem),
			}.SelectMany (Leaks).ToList ();

			Assert.AreEqual (0, leaks.Count, string.Join (Environment.NewLine, leaks));
		}

		[TestMethod]
		public void FSpotAssemblyDoesNotReferenceGtkGdk ()
		{
			string[] banned = {
				"gdk-sharp",
				"gtk-sharp",
				"glib-sharp",
				"atk-sharp",
				"pango-sharp",
				"gtk-sharp-beans",
				"Mono.Cairo",
			};

			var leaks = typeof (ImageBuffer).Assembly.GetReferencedAssemblies ()
				.Select (name => name.Name)
				.Where (name => banned.Any (b => string.Equals (name, b, StringComparison.OrdinalIgnoreCase)))
				.Distinct ()
				.OrderBy (name => name)
				.ToList ();

			Assert.AreEqual (0, leaks.Count, string.Join (", ", leaks));
		}

		[TestMethod]
		public void FSpotDllConfigMapsExactlyTheLibrariesFSpotImports ()
		{
			var configPath = Path.Combine (AppContext.BaseDirectory, "FSpot.dll.config");
			if (!File.Exists (configPath)) {
				Assert.Inconclusive ($"{configPath} is missing; copy FSpot.dll.config.in over it as the build does.");
				return;
			}

			var mapped = XDocument.Load (configPath)
				.Descendants ("dllmap")
				.Select (element => (string)element.Attribute ("dll"))
				.Distinct (StringComparer.Ordinal)
				.OrderBy (name => name, StringComparer.Ordinal)
				.ToList ();

			var imported = ImportedLibraries (typeof (ImageBuffer).Assembly);

			CollectionAssert.AreEqual (imported, mapped,
				$"dllmap drift: FSpot.dll.config maps [{string.Join (", ", mapped)}] "
				+ $"but FSpot.dll imports [{string.Join (", ", imported)}]");
		}

		static List<string> ImportedLibraries (Assembly assembly)
		{
			const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic
				| BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;

			Type[] types;
			try {
				types = assembly.GetTypes ();
			} catch (ReflectionTypeLoadException ex) {
				types = ex.Types.Where (type => type != null).ToArray ();
			}

			return types
				.SelectMany (type => type.GetMethods (flags))
				.Select (method => method.GetCustomAttribute<DllImportAttribute> ())
				.Where (attribute => attribute != null)
				.Select (attribute => attribute.Value)
				.Distinct (StringComparer.Ordinal)
				.OrderBy (name => name, StringComparer.Ordinal)
				.ToList ();
		}

		static List<string> Leaks (Type type)
		{
			const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
			var leaks = new List<string> ();

			foreach (var property in type.GetProperties (flags)) {
				if (TryGetType (property, p => p.PropertyType, out var propertyType, out var loadError))
					leaks.Add ($"{type.Name}.{property.Name} : {loadError}");
				else if (TryUsesPixbuf (propertyType, out var name))
					leaks.Add ($"{type.Name}.{property.Name} : {name}");
			}

			foreach (var method in type.GetMethods (flags)) {
				if (method.IsSpecialName)
					continue;
				try {
					leaks.AddRange (MethodLeaks (type, method));
				} catch (Exception ex) when (ex is FileNotFoundException || ex is TypeLoadException) {
					leaks.Add ($"{type.Name}.{method.Name}: {ex.Message}");
				}
			}

			return leaks;
		}

		static List<string> MethodLeaks (Type type, MethodInfo method)
		{
			var leaks = new List<string> ();
			if (TryGetType (method, m => m.ReturnType, out var returnType, out var returnError))
				leaks.Add ($"{type.Name}.{method.Name}() : {returnError}");
			else if (TryUsesPixbuf (returnType, out var returnName))
				leaks.Add ($"{type.Name}.{method.Name}() : {returnName}");
			foreach (var parameter in method.GetParameters ()) {
				if (TryGetType (parameter, p => p.ParameterType, out var parameterType, out var parameterError))
					leaks.Add ($"{type.Name}.{method.Name}({parameter.Name} : {parameterError})");
				else if (TryUsesPixbuf (parameterType, out var parameterName))
					leaks.Add ($"{type.Name}.{method.Name}({parameter.Name} : {parameterName})");
			}
			return leaks;
		}

		static bool TryGetType<T> (T member, Func<T, Type> read, out Type type, out string error)
		{
			try {
				type = read (member);
				error = null;
				return false;
			} catch (Exception ex) when (ex is FileNotFoundException || ex is TypeLoadException) {
				type = null;
				error = ex.Message;
				return true;
			}
		}

		static bool TryUsesPixbuf (Type type, out string name)
		{
			try {
				name = type.FullName;
				if (name == "Gdk.Pixbuf")
					return true;
				if (type.IsByRef || type.IsArray)
					return TryUsesPixbuf (type.GetElementType (), out name);
				return false;
			} catch (Exception ex) when (ex is FileNotFoundException || ex is TypeLoadException) {
				name = ex.Message;
				return name.IndexOf ("gdk-sharp", StringComparison.OrdinalIgnoreCase) >= 0
					|| name.IndexOf ("Gdk.Pixbuf", StringComparison.OrdinalIgnoreCase) >= 0;
			}
		}
	}
}
