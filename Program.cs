using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.IO;

namespace WeaverCore
{
    class Program
    {
        static void Main(string[] args)
        {
            string dllPath = args[0];
            string pdbPath = $"{Path.GetDirectoryName(dllPath)}/{Path.GetFileNameWithoutExtension(dllPath)}.pdb";
            var compiledAssembly = new CompiledAssembly()
            {
                Name = Path.GetFileNameWithoutExtension(dllPath),
                PeData = File.ReadAllBytes(dllPath),
                PdbData = File.ReadAllBytes(pdbPath),
                Defines = new string[0],
                References = new string[0],
            };

            var weaver = new Weaver();
            AssemblyDefinition assemblyDefinition = weaver.Weave(compiledAssembly);

            var pe = new MemoryStream();
            var pdb = new MemoryStream();

            var writerParameters = new WriterParameters
            {
                SymbolWriterProvider = new PortablePdbWriterProvider(),
                SymbolStream = pdb,
                WriteSymbols = true
            };

            assemblyDefinition?.Write(pe, writerParameters);

            File.WriteAllBytes(dllPath, pe.ToArray());
            File.WriteAllBytes(pdbPath, pdb.ToArray());
        }
    }
    class Weaver
    {
        public AssemblyDefinition Weave(CompiledAssembly compiledAssembly)
        {
            AssemblyDefinition assemblyDefinition = AssemblyDefinitionFor(compiledAssembly);
            ModuleDefinition module = assemblyDefinition.MainModule;
            foreach (TypeDefinition type in module.Types)
            {
                foreach (MethodDefinition method in type.Methods)
                {
                    if (method.Name == "_Debug_Weaver")
                    {
                        ILProcessor worker = method.Body.GetILProcessor();
                        Instruction top = method.Body.Instructions[0];

                        int now = DateTime.Now.Second + DateTime.Now.Minute * 60 + DateTime.Now.Hour * 3600;
                        worker.InsertBefore(top, worker.Create(OpCodes.Ldc_I4, now));
                        worker.InsertBefore(top, worker.Create(OpCodes.Ret));
                    }
                }
            }
            return assemblyDefinition;
        }

        public static AssemblyDefinition AssemblyDefinitionFor(CompiledAssembly compiledAssembly)
        {
            //var assemblyResolver = new PostProcessorAssemblyResolver(compiledAssembly);
            var assemblyResolver = new DefaultAssemblyResolver();
            var readerParameters = new ReaderParameters
            {
                SymbolStream = new MemoryStream(compiledAssembly.PdbData),
                SymbolReaderProvider = new PortablePdbReaderProvider(),
                AssemblyResolver = assemblyResolver,
                ReflectionImporterProvider = new PostProcessorReflectionImporterProvider(),
                ReadingMode = ReadingMode.Immediate
            };

            var assemblyDefinition = AssemblyDefinition.ReadAssembly(new MemoryStream(compiledAssembly.PeData), readerParameters);

            return assemblyDefinition;
        }
    }
    class CompiledAssembly
    {
        public string Name { get; set; }
        public string[] References { get; set; }
        public string[] Defines { get; set; }

        public byte[] PeData { get; set; }
        public byte[] PdbData { get; set; }
    }

    internal class PostProcessorReflectionImporterProvider : IReflectionImporterProvider
    {
        public IReflectionImporter GetReflectionImporter(ModuleDefinition module)
        {
            //return new PostProcessorReflectionImporter(module);
            return new DefaultReflectionImporter(module);
        }
    }
}
