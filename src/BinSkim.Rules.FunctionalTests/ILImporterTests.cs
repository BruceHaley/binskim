﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

using Microsoft.CodeAnalysis.CSharp;

using Xunit;

namespace Microsoft.CodeAnalysis.IL
{
    public class ILImporterTests
    {
        [Fact]
        public void DemonstrateSetup()
        {
            string path = typeof(ILImporterTests).GetTypeInfo().Assembly.Location;

            using (var stream = File.OpenRead(path))
            using (var peReader = new PEReader(stream))
            {
                var metadataReader = peReader.GetMetadataReader();
                var reference = MetadataReference.CreateFromFile(path);
                var compilation = CSharpCompilation.Create("_", references: new[] { reference });
                var target = (IAssemblySymbol)compilation.GetAssemblyOrModuleSymbol(reference);

                var type = target.GetTypeByMetadataName(typeof(ILImporterTests).FullName);
                var method = (IMethodSymbol)type.GetMembers().Single(m => m.Name == nameof(Scratch));
                var handle = (MethodDefinitionHandle)((IMetadataSymbol)method).MetadataHandle;
                var methodDef = metadataReader.GetMethodDefinition(handle);
                var methodBody = peReader.GetMethodBody(methodDef.RelativeVirtualAddress);

                var importer = new ILImporter(compilation, metadataReader, method, methodBody);
                var body = importer.Import();
            }
        }

        public int InstanceField;
        public static int StaticField;

        public void Scratch(string x, int y)
        {
            try
            {
                InstanceField = 42;
                StaticField = 42;

                int tmp = StaticMethod(y == 12 ? "hello" : "goodbye", InstanceField);
                InstanceMethod(x, StaticField);
            }
            catch (OverflowException)
            {
                try
                {
                    StaticMethod("goodbye", 99);
                }
                catch (InvalidOperationException)
                {

                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                InstanceMethod(x, y);
            }
        }

        public static int StaticMethod(object x, int y)
        {
            return 42;
        }

        public void InstanceMethod(string x, int y)
        {
        }
    }
}