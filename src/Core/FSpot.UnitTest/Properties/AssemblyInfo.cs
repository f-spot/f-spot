//
// AssemblyInfo.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

// These tests mutate process-wide state -- ImagingServices.Backend and Preferences --
// so they must not run in parallel. Saying so explicitly also answers MSTEST0001.
[assembly: DoNotParallelize]
