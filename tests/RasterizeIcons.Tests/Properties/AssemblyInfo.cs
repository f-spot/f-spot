// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

// Each test uses its own temp directory, so method-level parallelization is safe.
[assembly: Parallelize (Scope = ExecutionScope.MethodLevel)]
